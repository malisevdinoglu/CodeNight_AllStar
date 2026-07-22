"""AI Service — ai-db erişim katmanı (SQLAlchemy, parametrik sorgular - Core_Principles §10 SQLi kuralı).

`app.models` şeması İskender.md §4 sözleşmesidir. Yazma/okuma fonksiyonları burada toplanır ki
router'lar (app/routers/ai.py) ve RabbitMQ consumer'ı (app/consumers.py) aynı mantığı paylaşsın.

Idempotency notu: `classification_feedback.case_id` UNIQUE (İskender.md §4) - aynı `segment.overridden`
event'i RabbitMQ'nun "en az bir kez teslim" garantisiyle iki kez gelirse SELECT-then-INSERT + UNIQUE
constraint güvenlik ağı ile yut (dokümante edilmiş, portable - SQLite testlerinde de çalışır).
`score_adjustments` için de aynı SELECT-then-UPSERT deseni kullanılır (Postgres'e özgü
`ON CONFLICT` yerine tercih edildi: hem SQLite'ta test edilebilir hem de gerçek zamanlı, düşük
hacimli bu akış için yeterince güvenli - yarış koşulu riski dokümante edilmiştir).
"""

from __future__ import annotations

from uuid import UUID

from sqlalchemy import func, select
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.models import ClassificationFeedback, ModelMetadata, Prediction, ScoreAdjustment

PENALTY_STEP = 0.05
PENALTY_CAP = 0.50


# --------------------------------------------------------------------------
# predictions — /ai/recommend her çağrısında bir satır (doğruluk metriğinin girdisi)
# --------------------------------------------------------------------------


def record_prediction(
    session: Session,
    *,
    subscriber_id: UUID,
    campaign_id: UUID,
    recommendation_score: float,
    conversion_probability: float,
    predicted_segment: str,
    model_version: str,
) -> None:
    session.add(
        Prediction(
            subscriber_id=subscriber_id,
            campaign_id=campaign_id,
            recommendation_score=round(recommendation_score, 2),
            conversion_probability=round(conversion_probability, 2),
            predicted_segment=predicted_segment,
            model_version=model_version,
        )
    )
    session.commit()


# --------------------------------------------------------------------------
# classification_feedback — segment.overridden consumer'ı (case §5.4)
# --------------------------------------------------------------------------


def record_classification_feedback(
    session: Session,
    *,
    case_id: UUID,
    predicted_segment: str,
    corrected_segment: str,
    corrected_by: UUID,
) -> bool:
    """True: yeni kayıt yazıldı. False: bu case_id için zaten kayıt var (idempotent yut)."""
    already = session.execute(
        select(ClassificationFeedback.id).where(ClassificationFeedback.case_id == case_id)
    ).first()
    if already is not None:
        return False

    session.add(
        ClassificationFeedback(
            case_id=case_id,
            predicted_segment=predicted_segment,
            corrected_segment=corrected_segment,
            corrected_by=corrected_by,
        )
    )
    try:
        session.commit()
        return True
    except IntegrityError:
        # Yarış durumu: aynı case_id iki eşzamanlı consumer tarafından işlendi - UNIQUE koru.
        session.rollback()
        return False


# --------------------------------------------------------------------------
# score_adjustments — offer.responded (RET) consumer'ı (case §4.5)
# --------------------------------------------------------------------------


def bump_score_adjustment_penalty(
    session: Session,
    *,
    subscriber_id: UUID,
    campaign_type: str,
    step: float = PENALTY_STEP,
    cap: float = PENALTY_CAP,
) -> float:
    """Abone bir kampanya tipini reddettikçe o kombinasyonun cezası artar (öneri skorunu düşürür).

    Döner: güncel (yeni) penalty değeri.
    """
    row = session.execute(
        select(ScoreAdjustment).where(
            ScoreAdjustment.subscriber_id == subscriber_id,
            ScoreAdjustment.campaign_type == campaign_type,
        )
    ).scalar_one_or_none()

    if row is None:
        row = ScoreAdjustment(subscriber_id=subscriber_id, campaign_type=campaign_type, penalty=0)
        session.add(row)

    row.penalty = min(cap, float(row.penalty) + step)
    session.commit()
    return float(row.penalty)


def get_score_adjustment_penalty(session: Session, *, subscriber_id: UUID, campaign_type: str) -> float:
    """/ai/recommend'in öneri skorunu ayarlamak için okuduğu ceza katsayısı (yoksa 0)."""
    penalty = session.execute(
        select(ScoreAdjustment.penalty).where(
            ScoreAdjustment.subscriber_id == subscriber_id,
            ScoreAdjustment.campaign_type == campaign_type,
        )
    ).scalar_one_or_none()
    return float(penalty) if penalty is not None else 0.0


# --------------------------------------------------------------------------
# GET /ai/metrics/accuracy — genel + kategori kırılımı (+3 bonus)
# --------------------------------------------------------------------------


def compute_accuracy_metrics(session: Session) -> tuple[float, list[dict]]:
    """Doğruluk = 1 - (feedback sayısı / toplam prediction) — İskender.md §4.

    Henüz hiç prediction yoksa (demo yeni başladı) eğitim metriklerine (model_metadata) düşer,
    böylece ekran sıfır/boş görünmez.
    """
    totals = dict(
        session.execute(
            select(Prediction.predicted_segment, func.count()).group_by(Prediction.predicted_segment)
        ).all()
    )
    wrongs = dict(
        session.execute(
            select(ClassificationFeedback.predicted_segment, func.count()).group_by(
                ClassificationFeedback.predicted_segment
            )
        ).all()
    )

    fallback = _latest_training_accuracy(session)

    total_all = sum(totals.values())
    wrong_all = sum(wrongs.get(segment, 0) for segment in totals)
    overall = round(1 - wrong_all / total_all, 4) if total_all > 0 else fallback

    known_segments = {"YUKSEK_DEGER", "RISKLI_KAYIP", "YENI_ABONE", "PASIF"}
    all_segments = sorted(known_segments | set(totals) | set(wrongs))

    by_category = []
    for segment in all_segments:
        total = totals.get(segment, 0)
        wrong = wrongs.get(segment, 0)
        accuracy = round(1 - wrong / total, 4) if total > 0 else fallback
        by_category.append({"segment": segment, "accuracy": accuracy, "total": total})

    return overall, by_category


def _latest_training_accuracy(session: Session) -> float:
    row = session.execute(select(ModelMetadata).order_by(ModelMetadata.trained_at.desc())).scalars().first()
    return float(row.accuracy) if row is not None else 1.0
