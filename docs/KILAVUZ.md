# 🚀 CODENIGHT EKİP ÇALIŞMA VE GIT KILAVUZU
**ÖNEMLİ UYARI:** 12 saatlik süremiz var. Hata yapma lüksümüz yok. Lütfen ezbere işlem yapmayın, her adımda bu kılavuzu takip edin! Ana (hedef) branch'imiz **`master`**'dır.

## Ekip Rolleri
*   **Mali (Tech Lead):** Proje Mimarisinden, Backend iş mantığından ve PR Onaylarından sorumlu (Gatekeeper).
*   **İskender (DB):** Veritabanı modelleri, context ve konfigürasyonlardan sorumlu.
*   **Osman (Frontend):** UI, Arayüz Geliştirme ve API Tüketiminden sorumlu.

## 3 ALTIN KURAL
1.  **ASLA** `master` branch'inde kod yazılmaz!
2.  **ASLA** `master` branch'ine doğrudan push atılmaz!
3.  Kodunu GitHub'a göndermeden önce **KESİNLİKLE** güncel `master` kodlarını kendi kişisel çalışma dalına (branch) çekip kontrol edersin.

---

## 👨‍💻 BÖLÜM 1: GELİŞTİRİCİ AKIŞI (İskender & Osman & Mali)
*(YENİ KURAL: Herkes yarışma boyunca SADECE kendi adını taşıyan tek bir kişisel branch'te çalışacaktır. Başka branch açılmayacaktır!)*

### ADIM 1: İşe Başlamadan Önce (Kendi Odanı Aç ve Hep Orada Kal)
Yarışma başlar başlamaz herkes kendi adındaki dalı (branch) SADECE BİR KERE açıp içine girecek ve bütün gece oradan çıkmayacak.

```bash
# 1. Ana branch'ten en güncel kodları al
git checkout master
git pull origin master

# 2. SADECE 1 KERE: Kendi adınla branch aç ve içine gir (Örn: iskender-dev, osman-dev, mali-dev)
git checkout -b <kendi-adiniz>-dev


ADIM 2: Kodlama ve Kaydetme (Commit)
Kendi kişisel odanda işini yap, kodlarını yaz ve bitir.

# 1. Değişiklikleri Git'e ekle
git add .

# 2. Yaptığın işi anlatan kısa bir mesajla kaydet
git commit -m "Buraya ne yaptığını yaz (Örn: Login arayüzü tasarlandı)"




 ### ADIM 3: Güvenlik Kontrolü ve Push (EN KRİTİK ADIM)
Sen kod yazarken bir başkasının kodu master'a eklenmiş olabilir. Kodunu GitHub'a yollamadan önce çakışma (conflict) var mı diye kontrol et:

# Güncel master'ı kendi branch'ine entegre et
git pull origin master


-> Durum A (Sorun Yok): Terminal "Already up to date" veya sorunsuz birleşti mesajı verirse her şey yolunda. Hemen GitHub'a yolla:

git push origin <kendi-adiniz>-dev

-> Durum B (Çakışma/Conflict Var): Terminal "CONFLICT" hatası verirse, editöründe (Cursor/VS Code) kırmızı yanan çakışan dosyaları aç, doğrularını seç ve kaydet. Ardından şu komutlarla düzeltmeyi yolla:

git add .
git commit -m "Conflict çözüldü"
git push origin <kendi-adiniz>-dev



### ADIM 4: Pull Request (PR) Aç
1- GitHub web sitesine gir.
2- Ekrandaki yeşil renkli "Compare & pull request" butonuna bas.
3- Hedef branch'in master olduğundan emin ol.
4- "Create pull request" butonuna bas ve onaylaması için Mali'ye haber ver.
(ÖNEMLİ UYARI: Mali PR'ı onayladıktan sonra branch'i SİLMEYECEK. Mali onaylayana kadar yeni işe başlamayın! Onay geldikten sonra aynı branch'te kod yazmaya devam edebilirsiniz.)