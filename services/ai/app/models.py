"""AI Service — SQLAlchemy 2.0 modelleri (ai-db, PostgreSQL).

Iskender.md §4 şeması. Database-per-service: bu tablolar SADECE AI Service'e aittir;
subscriber_id / campaign_id / case_id başka servislerin kimlikleridir ama FK DEĞİLDİR
(cross-service FK yasağı — Core_Principles §2.1).

Doğruluk metriği = 1 - (feedback sayısı / toplam prediction).
Kategori kırılımı `predicted_segment` group by (+3 bonus verisi).
"""

import uuid
from datetime import datetime

from sqlalchemy import DateTime, Numeric, String, Text, func
from sqlalchemy.dialects.postgresql import UUID
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column


class Base(DeclarativeBase):
    pass


class Prediction(Base):
    """Her AI tahmini kaydedilir — doğruluk takibi ve model_version izlenebilirliği."""

    __tablename__ = "predictions"

    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    subscriber_id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), nullable=False, index=True)
    campaign_id: Mapped[uuid.UUID | None] = mapped_column(UUID(as_uuid=True), index=True)
    recommendation_score: Mapped[float] = mapped_column(Numeric(3, 2), nullable=False)
    conversion_probability: Mapped[float] = mapped_column(Numeric(3, 2), nullable=False)
    predicted_segment: Mapped[str] = mapped_column(String(20), nullable=False, index=True)
    model_version: Mapped[str] = mapped_column(String(20), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), nullable=False, server_default=func.now()
    )


class ClassificationFeedback(Base):
    """`segment.overridden` event'i buraya düşer: yanlış sınıflandırma kaydı (case §5.4)."""

    __tablename__ = "classification_feedback"

    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    case_id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), nullable=False, unique=True)
    predicted_segment: Mapped[str] = mapped_column(String(20), nullable=False)
    corrected_segment: Mapped[str] = mapped_column(String(20), nullable=False)
    corrected_by: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), nullable=False, server_default=func.now()
    )


class ScoreAdjustment(Base):
    """Abone 'ilgilenmiyorum' dedikçe o kampanya tipinin skoru düşer (case §4.5).

    Öneri skoru = model olasılığı - penalty (0'ın altına inmez).
    """

    __tablename__ = "score_adjustments"

    subscriber_id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True)
    campaign_type: Mapped[str] = mapped_column(String(20), primary_key=True)
    penalty: Mapped[float] = mapped_column(Numeric(3, 2), nullable=False, default=0)
    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), nullable=False, server_default=func.now(), onupdate=func.now()
    )


class ModelMetadata(Base):
    """Eğitilen her model versiyonunun künyesi — ml/metrics.json açılışta buraya seed'lenir."""

    __tablename__ = "model_metadata"

    version: Mapped[str] = mapped_column(String(20), primary_key=True)
    trained_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    accuracy: Mapped[float] = mapped_column(Numeric(5, 4), nullable=False)
    f1_macro: Mapped[float] = mapped_column(Numeric(5, 4), nullable=False)
    notes: Mapped[str | None] = mapped_column(Text)
