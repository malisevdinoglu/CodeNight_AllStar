"""AI Service — Core_Principles §5 hata sözleşmesi: {SERVIS}_{HTTP}_{SEBEP} kod formatı,
her zaman ApiResponse zarfı (success/data/error), stack trace ASLA sızdırılmaz.
"""

from __future__ import annotations

import logging

from fastapi import FastAPI, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

from app.services.model_service import ModelNotTrainedError, UnknownCampaignTypeError

logger = logging.getLogger("campaigncell.ai")


def _envelope(code: str, message: str, details: list[str] | None = None) -> dict:
    return {
        "success": False,
        "data": None,
        "error": {"code": code, "message": message, "details": details or []},
    }


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def _validation_error_handler(_: Request, exc: RequestValidationError) -> JSONResponse:
        details = [f"{'.'.join(str(loc) for loc in err['loc'])}: {err['msg']}" for err in exc.errors()]
        return JSONResponse(
            status_code=status.HTTP_400_BAD_REQUEST,
            content=_envelope("AI_400_VALIDATION", "İstek gövdesi doğrulanamadı.", details),
        )

    @app.exception_handler(UnknownCampaignTypeError)
    async def _unknown_campaign_type_handler(_: Request, exc: UnknownCampaignTypeError) -> JSONResponse:
        return JSONResponse(
            status_code=status.HTTP_400_BAD_REQUEST,
            content=_envelope("AI_400_UNKNOWN_CAMPAIGN_TYPE", str(exc)),
        )

    @app.exception_handler(ModelNotTrainedError)
    async def _model_not_trained_handler(_: Request, exc: ModelNotTrainedError) -> JSONResponse:
        logger.error("Model yuklenemedi: %s", exc)
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content=_envelope("AI_503_UNAVAILABLE", "AI modeli hazır değil, lütfen daha sonra tekrar deneyin."),
        )

    @app.exception_handler(Exception)
    async def _unhandled_exception_handler(_: Request, exc: Exception) -> JSONResponse:
        logger.exception("Beklenmeyen hata: %s", exc)
        return JSONResponse(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            content=_envelope("AI_500_INTERNAL", "Beklenmeyen bir hata oluştu."),
        )
