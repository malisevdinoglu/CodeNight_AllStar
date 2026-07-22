# AI Yaklaşım Dokümanı — CampaignCell

> Case §14.2 teslimat şartı: hangi yöntemi seçtik, neden, nasıl çalışıyor.
> Kendi veri setimizle model eğitiyoruz (+8 bonus) — eğitim verisi bu repoda: `services/ai/data/training_data.csv`.

## 1. Seçilen Yaklaşım

**Kendi eğittiğimiz klasik ML modeli (scikit-learn) + kural tabanlı destek (hibrit).**

- **Segment sınıflandırma:** kendi ürettiğimiz sentetik veriyle eğitilmiş sınıflandırıcı
  (`YUKSEK_DEGER | RISKLI_KAYIP | YENI_ABONE | PASIF`).
- **Öneri skorlama / dönüşüm tahmini:** kampanya tipi başına kabul olasılığı modeli
  (`accepted_*` etiketleri üzerinden) + geri bildirim cezası (`score_adjustments` tablosu:
  abone "ilgilenmiyorum" derse benzer kampanya tipinin skoru düşer — case §4.5).
- **Akıllı uzman ataması:** kural tabanlı skorlama formülü (ML gerektirmez, case §5.3):
  `skor = uzmanlik_eslesme × 0.5 + bosluk_orani × 0.3 + performans × 0.2`.

**Neden hibrit?** LLM API'ye bağımlılık yok (demo sırasında internet/quota riski sıfır),
model davranışı deterministik ve savunulabilir, eğitim verisi + süreç tamamen bizim
(+8 bonus şartını karşılar). Atama gibi net iş kuralı olan yerde ML kullanmamak
hem doğru mühendislik hem jüriye anlatması kolay.

## 2. Eğitim Verisi Üretimi (`services/ai/ml/generate_data.py`)

Gerçek abone verimiz olmadığı için **kural + gürültü** yöntemiyle 300 satır sentetik,
gerçekçi Türkçe abone profili ürettik (case önerisi min. 100; 300 kullandık).

### 2.1 Segment arketipleri

| Segment | Satır | Karakteristik |
|---|---|---|
| `YUKSEK_DEGER` | 80 | 400-900 TL harcama, 30-80 GB data, şikayet ≤1, aktif, sık ek paket alımı |
| `RISKLI_KAYIP` | 80 | Düşen kullanım (2-15 GB), şikayet 3+, 30-90 gün pasiflik → churn sinyali |
| `YENI_ABONE` | 70 | Tenure < 6 ay, orta kullanım |
| `PASIF` | 70 | 0.5-6 GB data, 60-180 gün aktivite yok, ek paket almıyor |

### 2.2 Kabul (accept/ret) etiketleri

Her satırda kampanya tipi başına `accepted_*` (0/1) etiketi var. Taban olasılıklar
segment eğilimini yansıtır (örn. `RISKLI_KAYIP` × `SADAKAT` = 0.68 — indirimle tutulur;
`PASIF` × `EK_PAKET` = 0.07 — çoğunlukla ret), sonra profil özellikleriyle ayarlanır:

- data > 25 GB → `EK_PAKET` +0.12 (yüksek kullanım ek pakete yatkın)
- şikayet ≥ 3 → `SADAKAT` +0.08
- harcama > 500 TL → `TARIFE_YUKSELTME` +0.10
- 60+ gün pasiflik → tüm tekliflere −0.05

### 2.3 Gürültü ve tekrar üretilebilirlik

- **%12 etiket gürültüsü:** her kabul etiketi %12 olasılıkla çevrilir → model %100
  accuracy'ye çıkamaz, gerçekçi öğrenme problemi oluşur (case şartı).
- **seed=42:** üretim deterministik; `python3 services/ai/ml/generate_data.py`
  her makinede birebir aynı CSV'yi üretir (MD5 doğrulandı).
- `past_acceptance_rate` alanı satırın kendi kabul etiketleriyle tutarlı üretilir (± 0.15 jitter).

## 3. Model Eğitimi (`services/ai/ml/train.py`)

> TODO — veri seti hazır, eğitim adımı sırada:
> scikit-learn pipeline, train/test split, accuracy + F1 (macro) raporu,
> `model.joblib` çıktısı, metrikler `model_metadata` tablosuna yazılır.

## 4. Doğruluk Takibi (runtime)

- Her tahmin `predictions` tablosuna yazılır (`model_version` ile).
- Uzman/süpervizör segmenti değiştirirse `segment.overridden` event'i →
  `classification_feedback` tablosu.
- Doğruluk = `1 − (feedback / toplam prediction)`; süpervizör dashboard'unda gösterilir.
- Kategori bazlı kırılım `predicted_segment` group by ile (+3 bonus).
