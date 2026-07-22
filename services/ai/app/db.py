"""AI Service — veritabanı bağlantısı ve oturum yönetimi.

Connection string SADECE env'den gelir (Core_Principles §10: secrets koda gömülmez).
`init_db()` açılışta tabloları oluşturur ve model_metadata'yı seed'ler —
`docker compose up` tek komut şartının Python tarafındaki karşılığı.
"""

import json
import os
import uuid
from datetime import datetime, timezone
from pathlib import Path

from sqlalchemy import create_engine, func
from sqlalchemy.orm import Session, sessionmaker

from app.models import Base, ClassificationFeedback, ModelMetadata, Prediction

DATABASE_URL = os.environ.get(
    "DATABASE_URL", "postgresql+psycopg://ai:ai@localhost:5435/ai_db"
)

engine = create_engine(DATABASE_URL, pool_pre_ping=True)
SessionLocal = sessionmaker(bind=engine, expire_on_commit=False)

METRICS_PATH = Path(__file__).resolve().parent.parent / "ml" / "metrics.json"

MODEL_VERSION = "1.0.0"

# Demo prediction seed'i — sabit GUID sozlesmesi (docs/SEED_DATA.md): abone i = b0000000-...-{i:012}.
# Segment sirasi Campaign subscriber_profiles seed'iyle birebir (3 YUKSEK_DEGER, 3 RISKLI_KAYIP, 2 YENI_ABONE, 2 PASIF).
_DEMO_PREDICTIONS = [
    ("YUKSEK_DEGER", 0.88, 0.82),
    ("YUKSEK_DEGER", 0.79, 0.71),
    ("YUKSEK_DEGER", 0.83, 0.76),
    ("RISKLI_KAYIP", 0.67, 0.55),
    ("RISKLI_KAYIP", 0.72, 0.60),
    ("RISKLI_KAYIP", 0.64, 0.51),
    ("YENI_ABONE", 0.70, 0.62),
    ("YENI_ABONE", 0.66, 0.58),
    ("PASIF", 0.61, 0.47),
    ("PASIF", 0.63, 0.49),
]


def _subscriber_id(one_based_index: int) -> uuid.UUID:
    """Identity/Campaign seed'iyle ayni sabit abone GUID'i (cross-service, FK degil)."""
    return uuid.UUID(f"b0000000-0000-0000-0000-{one_based_index:012d}")


def get_session():
    """FastAPI dependency: istek başına oturum, iş bitince kapat."""
    session: Session = SessionLocal()
    try:
        yield session
    finally:
        session.close()


def init_db() -> None:
    """Tabloları oluşturur, eğitilmiş modelin künyesini idempotent şekilde seed'ler."""
    Base.metadata.create_all(engine)

    if not METRICS_PATH.exists():
        return
    metrics = json.loads(METRICS_PATH.read_text(encoding="utf-8"))

    with SessionLocal() as session:
        if session.get(ModelMetadata, metrics["version"]) is None:
            session.add(
                ModelMetadata(
                    version=metrics["version"],
                    trained_at=datetime.now(timezone.utc),
                    accuracy=metrics["segment"]["accuracy"],
                    f1_macro=metrics["segment"]["f1_macro"],
                    notes="RandomForest segment + LogisticRegression kabul modelleri (seed=42)",
                )
            )
            session.commit()

        _seed_demo_predictions(session)


def _seed_demo_predictions(session: Session) -> None:
    """Demo prediction + 1 yanlis feedback seed'ler → dashboard'daki AI dogruluk metrigi
    bos/%100 kalmasin, gercekci ~%90 gorunsun (Iskender.md §6). Idempotent."""
    if session.query(func.count(Prediction.id)).scalar():
        return  # zaten seed'li → duplicate uretme

    now = datetime.now(timezone.utc)
    campaign_id = uuid.UUID("ca000000-0000-0000-0000-000000000001")  # Campaign seed'iyle ayni

    for i, (segment, score, conv) in enumerate(_DEMO_PREDICTIONS, start=1):
        session.add(
            Prediction(
                subscriber_id=_subscriber_id(i),
                campaign_id=campaign_id,
                recommendation_score=score,
                conversion_probability=conv,
                predicted_segment=segment,
                model_version=MODEL_VERSION,
                created_at=now,
            )
        )

    # Tek yanlis siniflandirma → dogruluk = 1 - (1 / 10) = %90 (uzman AI'i override etti).
    session.add(
        ClassificationFeedback(
            case_id=uuid.UUID("ca5e0000-0000-0000-0000-000000000001"),
            predicted_segment="PASIF",
            corrected_segment="RISKLI_KAYIP",
            corrected_by=uuid.UUID("e0000000-0000-0000-0000-000000000001"),  # uzman Deniz
            created_at=now,
        )
    )
    session.commit()
