"""CampaignCell AI Service — FastAPI giris noktasi.

Endpoint'ler Faz 6'da eklenir: /ai/recommend, /ai/classify, /ai/assign, /ai/metrics/accuracy.
Tum cevaplar Core_Principles §5 ApiResponse zarfina uyar.
"""

from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.db import init_db


@asynccontextmanager
async def lifespan(_: FastAPI):
    # Programatik migration + seed (Core_Principles §9: tek komut sarti) —
    # .NET servislerindeki MigrateAndSeedAsync()'in Python karsiligi.
    init_db()
    yield


app = FastAPI(
    title="CampaignCell AI Service",
    version="0.1.0",
    docs_url="/docs",
    lifespan=lifespan,
)


@app.get("/health")
def health() -> dict:
    return {
        "success": True,
        "data": {"status": "Healthy", "service": "ai"},
        "error": None,
    }
