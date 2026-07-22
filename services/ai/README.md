# AI Service (FastAPI)

CampaignCell — segment siniflandirma, teklif skoru ve uzman atama servisi (Python 3.12 + scikit-learn).

- `app/` — FastAPI uygulamasi (routers/, services/, models/)
- `ml/` — `generate_data.py` (Iskender), `train.py`, `model.joblib` (egitim compose build'inde DEGIL, onceden yapilir)
- `data/training_data.csv` — egitim verisi (repoda paylasilir, +8 bonus sarti)
- `tests/` — pytest

Endpoint'ler ve model detaylari: `docs/AI_APPROACH.md` (Faz 6).
