"""CampaignCell AI Service — FastAPI giris noktasi.

Endpoint'ler Faz 6'da eklenir: /ai/recommend, /ai/classify, /ai/assign, /ai/metrics/accuracy.
Tum cevaplar Core_Principles §5 ApiResponse zarfina uyar.
"""

from fastapi import FastAPI

app = FastAPI(
    title="CampaignCell AI Service",
    version="0.1.0",
    docs_url="/docs",
)


@app.get("/health")
def health() -> dict:
    return {
        "success": True,
        "data": {"status": "Healthy", "service": "ai"},
        "error": None,
    }
