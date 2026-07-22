"""AI Service — Mali.md §6 sözleşmesi: /api/v1/ai/recommend, /classify, /assign, /metrics/accuracy.

Tüm cevaplar `ApiResponse` zarfına sarılır (Core_Principles §5). Hatalar app.errors'taki global
handler'lara düşer (RequestValidationError → 400, ModelNotTrainedError → 503, diğer → 500).
"""

from __future__ import annotations

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app.db import get_session
from app.schemas import (
    AccuracyMetrics,
    ApiResponse,
    AssignmentScore,
    AssignRequest,
    ClassifyRequest,
    ClassifyResult,
    RecommendationResult,
    RecommendRequest,
)
from app.services import persistence
from app.services.model_service import ModelService, get_model_service
from app.services.scoring_strategy import get_scoring_strategy

router = APIRouter(prefix="/api/v1/ai", tags=["ai"])


@router.post("/recommend", response_model=ApiResponse[list[RecommendationResult]])
def recommend(
    body: RecommendRequest,
    session: Session = Depends(get_session),
    model: ModelService = Depends(get_model_service),
) -> dict:
    """Case §5.1: kampanya tipi başına kabul olasılığı → öneri skoru (skor ≥0.60 eşiği Campaign'de
    uygulanır, burası HAM skoru döner). Segment, doğruluk metriği için bir kez sınıflandırılıp her
    kampanya satırına predicted_segment olarak yazılır (predictions tablosu - accuracy girdisi).
    """
    profile = body.subscriber_profile
    predicted_segment, _confidence = model.classify_segment(profile)

    results: list[RecommendationResult] = []
    for campaign in body.campaigns:
        raw_probability = model.predict_acceptance(profile, campaign.type.value)
        penalty = persistence.get_score_adjustment_penalty(
            session, subscriber_id=profile.subscriber_id, campaign_type=campaign.type.value
        )
        recommendation_score = max(0.0, raw_probability - penalty)

        results.append(
            RecommendationResult(
                campaign_id=campaign.campaign_id,
                recommendation_score=round(recommendation_score, 2),
                conversion_probability=round(raw_probability, 2),
            )
        )
        persistence.record_prediction(
            session,
            subscriber_id=profile.subscriber_id,
            campaign_id=campaign.campaign_id,
            recommendation_score=recommendation_score,
            conversion_probability=raw_probability,
            predicted_segment=predicted_segment,
            model_version=model.version,
        )

    return {"success": True, "data": results, "error": None}


@router.post("/classify", response_model=ApiResponse[ClassifyResult])
def classify(
    body: ClassifyRequest,
    model: ModelService = Depends(get_model_service),
) -> dict:
    """Case §5.2: profil → segment + confidence. Saf sınıflandırma - predictions tablosuna
    yazmaz (o tablo recommendation_score/conversion_probability NOT NULL şart koşar; segment
    başına ayrı bir prediction akışı İskender.md §4 şemasında tanımlı değil - bkz. docs/AI_APPROACH.md).
    """
    segment, confidence = model.classify_segment(body.subscriber_profile)
    result = ClassifyResult(segment=segment, confidence=round(confidence, 2))
    return {"success": True, "data": result, "error": None}


@router.post("/assign", response_model=ApiResponse[list[AssignmentScore]])
def assign(body: AssignRequest) -> dict:
    """Case §5.3 / Mali.md §6: Strategy pattern skor formülü, azalan skora göre sıralı liste döner
    (Campaign.Application.Commands.AssignExpert en yüksek skorlu ilk elemanı seçer)."""
    strategy = get_scoring_strategy()
    scored = [
        AssignmentScore(expert_id=candidate.expert_id, score=strategy.score(body.case, candidate))
        for candidate in body.candidates
    ]
    scored.sort(key=lambda item: item.score, reverse=True)
    return {"success": True, "data": scored, "error": None}


@router.get("/metrics/accuracy", response_model=ApiResponse[AccuracyMetrics])
def accuracy(session: Session = Depends(get_session)) -> dict:
    """+3 bonus: genel doğruluk + segment bazlı kırılım (İskender.md §4 formülü)."""
    overall, by_category = persistence.compute_accuracy_metrics(session)
    result = AccuracyMetrics(overall=overall, by_category=by_category)
    return {"success": True, "data": result, "error": None}
