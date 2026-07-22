"""CampaignCell — model eğitimi.

İki ayrı görev, tek paket (model.joblib):
1. Segment sınıflandırma: profil → YUKSEK_DEGER | RISKLI_KAYIP | YENI_ABONE | PASIF
2. Kabul olasılığı: kampanya tipi başına ayrı ikili sınıflandırıcı
   (öneri skoru + dönüşüm tahmini bu olasılıklardan türetilir)

Deterministik: random_state=42. Metrikler stdout'a ve ml/metrics.json'a yazılır
(model_metadata tablosunun seed kaynağı). Süreç docs/AI_APPROACH.md §3'te anlatılır.
"""

import json
from pathlib import Path

import joblib
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, f1_score
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

SEED = 42
MODEL_VERSION = "1.0.0"

ML_DIR = Path(__file__).resolve().parent
DATA_PATH = ML_DIR.parent / "data" / "training_data.csv"

FEATURES = [
    "tenure_months",
    "avg_monthly_data_gb",
    "avg_monthly_call_minutes",
    "monthly_spend_tl",
    "package_purchase_count",
    "complaint_count",
    "days_since_last_activity",
    "past_acceptance_rate",
]
CAMPAIGN_TYPES = ["EK_PAKET", "TARIFE_YUKSELTME", "CIHAZ_FIRSATI", "SADAKAT"]


def train_segment_model(df: pd.DataFrame) -> tuple[RandomForestClassifier, dict]:
    """Görev 2 (case §5.2): abone davranış verisinden segment sınıflandırma."""
    X, y = df[FEATURES], df["label_segment"]
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=SEED, stratify=y
    )
    model = RandomForestClassifier(n_estimators=200, random_state=SEED)
    model.fit(X_train, y_train)
    pred = model.predict(X_test)
    metrics = {
        "accuracy": round(accuracy_score(y_test, pred), 4),
        "f1_macro": round(f1_score(y_test, pred, average="macro"), 4),
    }
    return model, metrics


def train_acceptance_models(df: pd.DataFrame) -> tuple[dict, dict]:
    """Görev 1 (case §5.1): kampanya tipi başına kabul olasılığı → öneri skoru girdisi.

    Logistic regression tercihi bilinçli: predict_proba çıktısı kalibre edilebilir,
    skorun 0-1 aralığında yorumlanabilir olması case şartı (0.60 eşiği, 0.80 önceliği).
    """
    models, metrics = {}, {}
    X = df[FEATURES]
    for ct in CAMPAIGN_TYPES:
        y = df[f"accepted_{ct.lower()}"]
        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=0.2, random_state=SEED, stratify=y
        )
        pipe = Pipeline([
            ("scaler", StandardScaler()),
            ("clf", LogisticRegression(max_iter=1000, random_state=SEED)),
        ])
        pipe.fit(X_train, y_train)
        pred = pipe.predict(X_test)
        models[ct] = pipe
        metrics[ct] = {
            "accuracy": round(accuracy_score(y_test, pred), 4),
            "f1": round(f1_score(y_test, pred), 4),
        }
    return models, metrics


def main() -> None:
    df = pd.read_csv(DATA_PATH)
    print(f"{len(df)} satır yüklendi ← {DATA_PATH.name}")

    segment_model, segment_metrics = train_segment_model(df)
    print(f"Segment modeli   → accuracy {segment_metrics['accuracy']}, f1_macro {segment_metrics['f1_macro']}")

    acceptance_models, acceptance_metrics = train_acceptance_models(df)
    for ct, m in acceptance_metrics.items():
        print(f"Kabul modeli {ct:18s} → accuracy {m['accuracy']}, f1 {m['f1']}")

    bundle = {
        "version": MODEL_VERSION,
        "feature_names": FEATURES,
        "segment_model": segment_model,
        "acceptance_models": acceptance_models,
    }
    joblib.dump(bundle, ML_DIR / "model.joblib")

    metrics = {
        "version": MODEL_VERSION,
        "segment": segment_metrics,
        "acceptance": acceptance_metrics,
    }
    (ML_DIR / "metrics.json").write_text(json.dumps(metrics, indent=2) + "\n", encoding="utf-8")
    print(f"model.joblib + metrics.json yazıldı → {ML_DIR}")


if __name__ == "__main__":
    main()
