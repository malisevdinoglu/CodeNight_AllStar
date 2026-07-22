"""AI Service — eğitilmiş model bundle'ının (ml/model.joblib) runtime servisi.

ml/train.py bundle'ı şu şekilde yazar:
    {"version", "feature_names", "segment_model", "acceptance_models": {campaign_type: pipeline}}

- segment_model: RandomForestClassifier, ham (ölçeklenmemiş) FEATURES üstünde eğitildi,
  classes_ zaten case dokümanının Türkçe UPPER_SNAKE segment adları (train.py'de y=label_segment
  sütunu doğrudan bu string'ler) - ayrıca bir çeviri/mapping GEREKMEZ.
- acceptance_models[campaign_type]: StandardScaler + LogisticRegression pipeline'ı, predict_proba
  ile [0,1] aralığında kalibre edilebilir olasılık döner (case §5.1 şartı - skor eşiği 0.60/0.80).

Model process-başına BİR KEZ yüklenir (joblib.load I/O maliyeti var); `get_model_service()`
thread-safe lazy singleton döndürür - her istek için diskten okuma YASAK (demo gecikmesi riski).
"""

from __future__ import annotations

import os
import threading
from pathlib import Path
from typing import Any

import joblib
import pandas as pd

_SERVICE_ROOT = Path(__file__).resolve().parent.parent.parent


def _resolve_model_path() -> Path:
    """MODEL_PATH env (.env.example) verilmişse onu kullanır - göreli ise servis köküne göre çözülür."""
    override = os.environ.get("MODEL_PATH")
    if not override:
        return _SERVICE_ROOT / "ml" / "model.joblib"
    path = Path(override)
    return path if path.is_absolute() else _SERVICE_ROOT / path


DEFAULT_MODEL_PATH = _resolve_model_path()


class ModelNotTrainedError(RuntimeError):
    """ml/model.joblib bulunamadı/yüklenemedi - `python ml/train.py` önceden çalıştırılmalı."""


class UnknownCampaignTypeError(ValueError):
    """acceptance_models içinde olmayan bir kampanya tipi istendi."""


class ModelService:
    """Segment sınıflandırma + kampanya tipi başına kabul olasılığı tahmini."""

    def __init__(self, model_path: Path = DEFAULT_MODEL_PATH) -> None:
        if not model_path.exists():
            raise ModelNotTrainedError(
                f"Model dosyası bulunamadı: {model_path}. Önce 'python ml/train.py' çalıştırılmalı "
                "(Core_Principles §6: eğitim compose build'inde DEĞİL, önceden yapılır)."
            )
        bundle: dict[str, Any] = joblib.load(model_path)
        self.version: str = bundle["version"]
        self.feature_names: list[str] = list(bundle["feature_names"])
        self._segment_model = bundle["segment_model"]
        self._acceptance_models: dict[str, Any] = bundle["acceptance_models"]

    def _feature_frame(self, profile: Any) -> pd.DataFrame:
        """`profile`, feature_names'teki her alanı snake_case attribute olarak taşıyan herhangi
        bir nesne olabilir (app.schemas.SubscriberProfile). Sıra bundle'daki feature_names'e göre
        sabitlenir - eğitimdeki sütun sırasıyla farklılık modelin sessizce yanlış tahmin üretmesine
        yol açar, bu yüzden isim bazlı DataFrame kullanılır (pozisyonel liste DEĞİL).
        """
        row = {name: getattr(profile, name) for name in self.feature_names}
        return pd.DataFrame([row], columns=self.feature_names)

    def classify_segment(self, profile: Any) -> tuple[str, float]:
        """Profil → (segment, confidence). confidence = en olası sınıfın predict_proba değeri."""
        frame = self._feature_frame(profile)
        proba = self._segment_model.predict_proba(frame)[0]
        classes = self._segment_model.classes_
        best_index = int(proba.argmax())
        return str(classes[best_index]), float(proba[best_index])

    def predict_acceptance(self, profile: Any, campaign_type: str) -> float:
        """Profil + kampanya tipi → kabul olasılığı P(accepted=1), [0,1] aralığında."""
        pipeline = self._acceptance_models.get(campaign_type)
        if pipeline is None:
            raise UnknownCampaignTypeError(f"Bilinmeyen kampanya tipi: {campaign_type}")

        frame = self._feature_frame(profile)
        proba = pipeline.predict_proba(frame)[0]
        classes = list(pipeline.classes_)
        positive_index = classes.index(1)
        return float(proba[positive_index])


_instance: ModelService | None = None
_lock = threading.Lock()


def get_model_service() -> ModelService:
    """FastAPI dependency: lazy singleton, double-checked locking (uvicorn çoklu worker/thread güvenli)."""
    global _instance
    if _instance is None:
        with _lock:
            if _instance is None:
                _instance = ModelService()
    return _instance


def reset_model_service_for_tests() -> None:
    """Testlerde farklı bir model_path ile yeniden yüklemek için singleton'ı sıfırlar."""
    global _instance
    with _lock:
        _instance = None
