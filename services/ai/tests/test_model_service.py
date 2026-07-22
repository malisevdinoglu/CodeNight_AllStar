"""ml/model.joblib (repoya commit'li, seed=42) üstünde gerçek tahmin testleri.

Mali.md §6 "hardcoded görünme riskine karşı" şartı: iki farklı profilin iki farklı skor
üretmesi burada doğrulanır (demo örneğinin unit test karşılığı).
"""

from uuid import uuid4

import pytest

from app.schemas import SubscriberProfile
from app.services.model_service import ModelService, UnknownCampaignTypeError


@pytest.fixture(scope="module")
def model() -> ModelService:
    return ModelService()


def _profile(**overrides) -> SubscriberProfile:
    base = dict(
        subscriber_id=uuid4(),
        current_plan="Platinum 40GB",
        tenure_months=24,
        avg_monthly_data_gb=45.0,
        avg_monthly_call_minutes=600,
        monthly_spend_tl=550.0,
        package_purchase_count=3,
        complaint_count=0,
        days_since_last_activity=2,
        past_acceptance_rate=0.65,
    )
    base.update(overrides)
    return SubscriberProfile(**base)


def test_classify_returns_known_segment_and_confidence_in_range(model: ModelService) -> None:
    segment, confidence = model.classify_segment(_profile())

    assert segment in {"YUKSEK_DEGER", "RISKLI_KAYIP", "YENI_ABONE", "PASIF"}
    assert 0.0 <= confidence <= 1.0


def test_predict_acceptance_is_a_probability(model: ModelService) -> None:
    probability = model.predict_acceptance(_profile(), "EK_PAKET")

    assert 0.0 <= probability <= 1.0


def test_two_different_profiles_yield_different_scores(model: ModelService) -> None:
    high_value = _profile(
        avg_monthly_data_gb=70.0,
        monthly_spend_tl=800.0,
        complaint_count=0,
        days_since_last_activity=1,
        past_acceptance_rate=0.8,
    )
    passive = _profile(
        avg_monthly_data_gb=2.0,
        monthly_spend_tl=100.0,
        complaint_count=1,
        days_since_last_activity=120,
        tenure_months=60,
        package_purchase_count=0,
        past_acceptance_rate=0.1,
    )

    score_high_value = model.predict_acceptance(high_value, "EK_PAKET")
    score_passive = model.predict_acceptance(passive, "EK_PAKET")

    assert score_high_value != score_passive
    assert score_high_value > score_passive


def test_riskli_kayip_profile_prefers_sadakat_over_tarife_yukseltme(model: ModelService) -> None:
    """Iskender.md §5 kural seti: RISKLI_KAYIP SADAKAT indirimine, TARIFE_YUKSELTME'ye göre daha yatkın."""
    churn_risk = _profile(
        avg_monthly_data_gb=8.0,
        avg_monthly_call_minutes=120,
        monthly_spend_tl=200.0,
        complaint_count=4,
        days_since_last_activity=45,
        package_purchase_count=0,
        past_acceptance_rate=0.2,
    )

    sadakat_score = model.predict_acceptance(churn_risk, "SADAKAT")
    tarife_score = model.predict_acceptance(churn_risk, "TARIFE_YUKSELTME")

    assert sadakat_score > tarife_score


def test_unknown_campaign_type_raises(model: ModelService) -> None:
    with pytest.raises(UnknownCampaignTypeError):
        model.predict_acceptance(_profile(), "BILINMEYEN_TIP")
