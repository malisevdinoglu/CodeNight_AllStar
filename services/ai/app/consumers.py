"""AI Service — RabbitMQ (aio-pika) tüketicisi.

Core_Principles §8: topic exchange "campaigncell.events", routing key = event_type, MassTransit
tarafı `UseRawJsonSerializer()` ile yayınladığı için gövde SADE JSON'dur (MassTransit zarfı yok).

Dinlenen event'ler (Mali.md §6):
- `segment.overridden` → classification_feedback (doğruluk metriğinin girdisi)
- `offer.responded` (RET) → score_adjustments ceza katsayısı (case §4.5)

Mesaj işleme mantığı (`dispatch_event`) aio-pika bağlantısından tamamen bağımsız tutulur ki
gerçek bir RabbitMQ olmadan da unit test edilebilsin.
"""

from __future__ import annotations

import asyncio
import json
import logging
import os
from typing import Any, Callable

import aio_pika
from aio_pika.abc import AbstractIncomingMessage
from sqlalchemy.orm import Session

from app.db import SessionLocal
from app.services import persistence

logger = logging.getLogger("campaigncell.ai.consumer")

EVENTS_EXCHANGE = "campaigncell.events"
QUEUE_NAME = "ai-service.events"

SEGMENT_OVERRIDDEN = "segment.overridden"
OFFER_RESPONDED = "offer.responded"

ROUTING_KEYS = (SEGMENT_OVERRIDDEN, OFFER_RESPONDED)


def handle_segment_overridden(session: Session, payload: dict[str, Any]) -> None:
    """Core_Principles §8 payload: case_id, predicted_segment, corrected_segment, changed_by."""
    persistence.record_classification_feedback(
        session,
        case_id=payload["case_id"],
        predicted_segment=payload["predicted_segment"],
        corrected_segment=payload["corrected_segment"],
        corrected_by=payload["changed_by"],
    )


def handle_offer_responded(session: Session, payload: dict[str, Any]) -> None:
    """Case §4.5: sadece RET skor düşürme katsayısını günceller; KABUL yok sayılır."""
    if payload.get("response") != "RET":
        return
    persistence.bump_score_adjustment_penalty(
        session,
        subscriber_id=payload["subscriber_id"],
        campaign_type=payload["campaign_type"],
    )


_HANDLERS: dict[str, Callable[[Session, dict[str, Any]], None]] = {
    SEGMENT_OVERRIDDEN: handle_segment_overridden,
    OFFER_RESPONDED: handle_offer_responded,
}


def dispatch_event(session: Session, event_type: str, payload: dict[str, Any]) -> None:
    """Pure yönlendirme fonksiyonu — aio-pika'dan bağımsız, unit test edilebilir."""
    handler = _HANDLERS.get(event_type)
    if handler is None:
        logger.debug("Ilgilenilmeyen event_type, atlaniyor: %s", event_type)
        return
    handler(session, payload)


def _process_message_sync(raw_body: bytes) -> None:
    envelope = json.loads(raw_body)
    event_type = envelope.get("event_type")
    payload = envelope.get("payload", {})
    session = SessionLocal()
    try:
        dispatch_event(session, event_type, payload)
    finally:
        session.close()


class EventConsumer:
    """FastAPI lifespan tarafından start/stop edilen arka plan RabbitMQ tüketicisi.

    SQLAlchemy Session senkrondur — event loop'u bloklamamak için her mesaj `asyncio.to_thread`
    ile ayrı thread'e taşınır (bu akışın hacminde ekstra async DB sürücüsü gerekmez).
    """

    def __init__(self) -> None:
        self._connection: Any = None
        self._channel: Any = None

    async def start(self) -> None:
        host = os.environ.get("RABBITMQ_HOST", "rabbitmq")
        user = os.environ.get("RABBITMQ_USER", "guest")
        password = os.environ.get("RABBITMQ_PASSWORD", "guest")

        self._connection = await aio_pika.connect_robust(host=host, login=user, password=password)
        self._channel = await self._connection.channel()
        await self._channel.set_qos(prefetch_count=10)

        exchange = await self._channel.declare_exchange(
            EVENTS_EXCHANGE, aio_pika.ExchangeType.TOPIC, durable=True
        )
        queue = await self._channel.declare_queue(QUEUE_NAME, durable=True)
        for routing_key in ROUTING_KEYS:
            await queue.bind(exchange, routing_key=routing_key)

        await queue.consume(self._on_message)
        logger.info("RabbitMQ consumer basladi (exchange=%s, queue=%s)", EVENTS_EXCHANGE, QUEUE_NAME)

    async def stop(self) -> None:
        if self._connection is not None:
            await self._connection.close()
            logger.info("RabbitMQ consumer durduruldu.")

    async def _on_message(self, message: AbstractIncomingMessage) -> None:
        async with message.process(requeue=False):
            try:
                await asyncio.to_thread(_process_message_sync, message.body)
            except Exception:
                logger.exception(
                    "Event isleme hatasi (routing_key=%s) - mesaj reddediliyor.", message.routing_key
                )
                raise
