"""CampaignCell — sentetik AI eğitim verisi üretici.

Iskender.md §5 sözleşmesi:
- 300 satır sentetik Türkçe abone profili, 4 segment arketipi (kural + gürültü).
- Kolonlar: subscriber_profiles alanları + label_segment + kampanya tipi başına accepted (0/1).
- Deterministik: seed=42 → aynı CSV her makinede yeniden üretilebilir.
- Çıktı: services/ai/data/training_data.csv (repoya commit'lenir, +8 bonus şartı).

Üretim mantığı docs/AI_APPROACH.md'de anlatılır.
"""

import csv
import random
from pathlib import Path

SEED = 42
NOISE_RATE = 0.12  # %12 etiket gürültüsü: model %100 çıkmasın (gerçekçilik şartı)

SEGMENT_COUNTS = {
    "YUKSEK_DEGER": 80,
    "RISKLI_KAYIP": 80,
    "YENI_ABONE": 70,
    "PASIF": 70,
}

CAMPAIGN_TYPES = ["EK_PAKET", "TARIFE_YUKSELTME", "CIHAZ_FIRSATI", "SADAKAT"]

PLANS = {
    "YUKSEK_DEGER": ["Platinum 40GB", "Platinum 60GB", "Red Elite 50GB"],
    "RISKLI_KAYIP": ["Standart 15GB", "GNC 20GB", "Ekonomik 10GB"],
    "YENI_ABONE": ["GNC 20GB", "Hoşgeldin 25GB", "Standart 15GB"],
    "PASIF": ["Ekonomik 5GB", "Ekonomik 10GB", "Standart 15GB"],
}

# Segment bazlı kabul eğilimleri (Iskender.md §5'teki kural seti):
# yüksek data kullanan EK_PAKET'e, RISKLI_KAYIP SADAKAT indirimine yatkın, PASIF çoğunlukla RET.
BASE_ACCEPT_PROB = {
    "YUKSEK_DEGER": {"EK_PAKET": 0.72, "TARIFE_YUKSELTME": 0.58, "CIHAZ_FIRSATI": 0.50, "SADAKAT": 0.38},
    "RISKLI_KAYIP": {"EK_PAKET": 0.14, "TARIFE_YUKSELTME": 0.08, "CIHAZ_FIRSATI": 0.28, "SADAKAT": 0.68},
    "YENI_ABONE":   {"EK_PAKET": 0.45, "TARIFE_YUKSELTME": 0.32, "CIHAZ_FIRSATI": 0.52, "SADAKAT": 0.28},
    "PASIF":        {"EK_PAKET": 0.07, "TARIFE_YUKSELTME": 0.05, "CIHAZ_FIRSATI": 0.14, "SADAKAT": 0.24},
}


def _profile(segment: str, rng: random.Random) -> dict:
    """Segment arketipine uygun kullanım profili üretir (Iskender.md §5 aralıkları)."""
    if segment == "YUKSEK_DEGER":
        return {
            "tenure_months": rng.randint(12, 96),
            "avg_monthly_data_gb": round(rng.uniform(30, 80), 2),
            "avg_monthly_call_minutes": rng.randint(400, 1500),
            "monthly_spend_tl": round(rng.uniform(400, 900), 2),
            "package_purchase_count": rng.randint(2, 8),
            "complaint_count": rng.randint(0, 1),
            "days_since_last_activity": rng.randint(0, 5),
        }
    if segment == "RISKLI_KAYIP":
        return {
            "tenure_months": rng.randint(6, 72),
            "avg_monthly_data_gb": round(rng.uniform(2, 15), 2),  # düşen kullanım
            "avg_monthly_call_minutes": rng.randint(20, 250),
            "monthly_spend_tl": round(rng.uniform(120, 350), 2),
            "package_purchase_count": rng.randint(0, 1),
            "complaint_count": rng.randint(3, 8),  # şikayet 3+ churn sinyali
            "days_since_last_activity": rng.randint(30, 90),
        }
    if segment == "YENI_ABONE":
        return {
            "tenure_months": rng.randint(0, 5),  # tenure < 6 ay
            "avg_monthly_data_gb": round(rng.uniform(8, 30), 2),
            "avg_monthly_call_minutes": rng.randint(100, 600),
            "monthly_spend_tl": round(rng.uniform(150, 450), 2),
            "package_purchase_count": rng.randint(0, 2),
            "complaint_count": rng.randint(0, 2),
            "days_since_last_activity": rng.randint(0, 14),
        }
    # PASIF
    return {
        "tenure_months": rng.randint(12, 120),
        "avg_monthly_data_gb": round(rng.uniform(0.5, 6), 2),
        "avg_monthly_call_minutes": rng.randint(0, 120),
        "monthly_spend_tl": round(rng.uniform(80, 200), 2),
        "package_purchase_count": 0,  # ek paket almıyor
        "complaint_count": rng.randint(0, 2),
        "days_since_last_activity": rng.randint(60, 180),
    }


def _accept_prob(segment: str, campaign_type: str, profile: dict) -> float:
    """Taban olasılığı profil özellikleriyle ayarlar — etiketler salt segmente değil davranışa da bağlı."""
    p = BASE_ACCEPT_PROB[segment][campaign_type]
    if campaign_type == "EK_PAKET" and profile["avg_monthly_data_gb"] > 25:
        p += 0.12  # yüksek data kullanan ek pakete yatkın
    if campaign_type == "SADAKAT" and profile["complaint_count"] >= 3:
        p += 0.08  # şikayetçi abone indirimle tutulur
    if campaign_type == "TARIFE_YUKSELTME" and profile["monthly_spend_tl"] > 500:
        p += 0.10
    if profile["days_since_last_activity"] > 60:
        p -= 0.05  # uzun süredir pasif → her teklife soğuk
    return min(max(p, 0.02), 0.95)


def generate(out_path: Path) -> None:
    rng = random.Random(SEED)

    gsm_pool = rng.sample(range(300_000_000, 600_000_000), sum(SEGMENT_COUNTS.values()))
    rows = []
    for segment, count in SEGMENT_COUNTS.items():
        for _ in range(count):
            profile = _profile(segment, rng)
            accepted = {}
            for ct in CAMPAIGN_TYPES:
                label = 1 if rng.random() < _accept_prob(segment, ct, profile) else 0
                if rng.random() < NOISE_RATE:  # gürültü: etiketi çevir
                    label = 1 - label
                accepted[f"accepted_{ct.lower()}"] = label

            accept_ratio = sum(accepted.values()) / len(CAMPAIGN_TYPES)
            past_rate = min(max(accept_ratio + rng.uniform(-0.15, 0.15), 0.0), 1.0)

            rows.append({
                "gsm_number": f"5{gsm_pool.pop():09d}",
                "current_plan": rng.choice(PLANS[segment]),
                **profile,
                "past_acceptance_rate": round(past_rate, 2),
                "label_segment": segment,
                **accepted,
            })

    rng.shuffle(rows)  # segment blokları ardışık kalmasın

    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    print(f"{len(rows)} satır yazıldı → {out_path}")
    for seg, cnt in SEGMENT_COUNTS.items():
        acc = [r for r in rows if r["label_segment"] == seg]
        avg = sum(sum(r[f"accepted_{ct.lower()}"] for ct in CAMPAIGN_TYPES) for r in acc) / (len(acc) * 4)
        print(f"  {seg:14s} {cnt:3d} satır | ort. kabul oranı {avg:.2f}")


if __name__ == "__main__":
    generate(Path(__file__).resolve().parent.parent / "data" / "training_data.csv")
