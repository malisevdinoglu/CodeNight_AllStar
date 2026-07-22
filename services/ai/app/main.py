"""CampaignCell AI Service — FastAPI giris noktasi.

Faz 6: /api/v1/ai/recommend, /classify, /assign, /metrics/accuracy (app/routers/ai.py) +
segment.overridden / offer.responded RabbitMQ tuketicisi (app/consumers.py).
Tum cevaplar Core_Principles §5 ApiResponse zarfina uyar (app/errors.py global handler'lar).
"""

import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.consumers import EventConsumer
from app.db import init_db
from app.errors import register_exception_handlers
from app.routers.ai import router as ai_router
from app.services.model_service import get_model_service

# Python'un kok logger seviyesi varsayilan WARNING'dir - uvicorn kendi logger'larini
# (uvicorn/uvicorn.error/uvicorn.access) yapilandirir ama digerlerine dokunmaz. Bu satir
# olmadan asagidaki INFO seviyeli loglar ("consumer basladi" vb.) container ciktisinda
# GORUNMEZ - .NET taraflarindaki Serilog'un varsayilan INFO seviyesiyle tutarli davranmak icin.
logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")

logger = logging.getLogger("campaigncell.ai")

_consumer = EventConsumer()


@asynccontextmanager
async def lifespan(_: FastAPI):
    # Programatik migration + seed (Core_Principles §9: tek komut sarti) —
    # .NET servislerindeki MigrateAndSeedAsync()'in Python karsiligi.
    init_db()

    # Model.joblib'i acilista bir kez yukle (ilk istekte gecikme olmasin, demo riski sifirlanir).
    # Yuklenemezse servis yine ayaga kalkar - /health yesil kalir, endpoint'ler AI_503_UNAVAILABLE doner.
    try:
        get_model_service()
    except Exception:
        logger.exception("Model yuklenemedi - servis modelsiz ayaga kalkiyor, endpoint'ler 503 donecek.")

    # RabbitMQ tuketicisi: baglanamazsa REST endpoint'leri (recommend/classify/assign/metrics)
    # yine calisir - sadece feedback dongusu (classification_feedback/score_adjustments) durur.
    try:
        await _consumer.start()
    except Exception:
        logger.exception("RabbitMQ consumer baslatilamadi - REST endpoint'leri yine de calisiyor.")

    yield

    await _consumer.stop()


app = FastAPI(
    title="CampaignCell AI Service",
    version="0.1.0",
    docs_url="/docs",
    lifespan=lifespan,
)

register_exception_handlers(app)
app.include_router(ai_router)


@app.get("/health")
def health() -> dict:
    return {
        "success": True,
        "data": {"status": "Healthy", "service": "ai"},
        "error": None,
    }
