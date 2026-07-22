"""AI Service — veritabanı bağlantısı ve oturum yönetimi.

Connection string SADECE env'den gelir (Core_Principles §10: secrets koda gömülmez).
`init_db()` açılışta tabloları oluşturur ve model_metadata'yı seed'ler —
`docker compose up` tek komut şartının Python tarafındaki karşılığı.
"""

import json
import os
from datetime import datetime, timezone
from pathlib import Path

from sqlalchemy import create_engine
from sqlalchemy.orm import Session, sessionmaker

from app.models import Base, ModelMetadata

DATABASE_URL = os.environ.get(
    "DATABASE_URL", "postgresql+psycopg://ai:ai@localhost:5435/ai_db"
)

engine = create_engine(DATABASE_URL, pool_pre_ping=True)
SessionLocal = sessionmaker(bind=engine, expire_on_commit=False)

METRICS_PATH = Path(__file__).resolve().parent.parent / "ml" / "metrics.json"


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
        if session.get(ModelMetadata, metrics["version"]) is not None:
            return  # idempotent: iki kez up → duplicate yok
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
