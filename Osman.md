# Osman — Frontend / UI / Servis Tüketimi Yol Haritası

> Önce `Core_Principles.md` oku. Bu dosya senin adım adım görev listen.
> Stack: React 18 + Vite + TypeScript, TanStack Query, Zustand, Tailwind, Recharts, react-hot-toast, @microsoft/signalr, zod, MSW (mock).
> **Altın kural:** Backend hazır olmadan MSW mock'larıyla tüm ekranları bitir. Mock'lar Core_Principles §5 zarfına ve bu dosyadaki DTO'lara birebir uyarsa, gün sonunda tek satır değişiklikle gerçek API'ye geçersin (`VITE_USE_MOCKS=false`).

---

## 1. Proje Yapısı

```
frontend/src/
├─ api/
│  ├─ client.ts          # axios instance: baseURL=VITE_API_URL (http://localhost:8080/api/v1)
│  ├─ auth.api.ts  campaign.api.ts  case.api.ts  game.api.ts  dashboard.api.ts  admin.api.ts
│  └─ types.ts           # TÜM DTO tipleri (aşağıdaki sözleşme) — tek dosya, tek doğru kaynak
├─ mocks/                # MSW handlers + fixtures (İskender'in seed'iyle aynı isim/değerler!)
├─ stores/
│  ├─ auth.store.ts      # Zustand: user, accessToken, role — persist YOK (güvenlik), refresh cookie/memory
│  └─ ui.store.ts        # toast kuyruğu, modal state
├─ hooks/                # useCases.ts, useOffers.ts, useLeaderboard.ts ... (TanStack Query sarmalayıcıları)
├─ components/
│  ├─ ui/                # Button, Card, Badge, Modal, Spinner, EmptyState, ErrorState, Table, StarRating
│  ├─ layout/            # AppShell, Sidebar (role göre menü), Topbar
│  └─ domain/            # OfferCard, CaseCard, SlaCountdown, SegmentBadge, PriorityBadge,
│                        #   LevelFrame (bronz/gümüş/altın/platin çerçeve), BadgeToast, LeaderboardTable
├─ pages/
│  ├─ auth/    LoginPage (2 sekme: GSM+OTP | e-posta+şifre), RegisterPage
│  ├─ subscriber/  OffersPage, OfferDetailPage (kabul/ret + yıldız), MyCampaignsPage
│  ├─ expert/  MyCasesPage (öncelik sıralı), CaseDetailPage (state akışı + AI tahmini + not), GameProfilePage
│  ├─ supervisor/  DashboardPage, QueuePage (manuel atama), CasesPage (tümü)
│  └─ admin/   StaffPage (personel oluştur), AuditLogsPage
└─ router.tsx            # role-based guard: MUSTERI→/offers, PERSONEL→/cases, SUPERVIZOR→/dashboard, ADMIN→/admin
```

## 2. API Sözleşmesi (mock'ların birebir uyacağı DTO'lar)

Her yanıt `{ success, data, error }` zarfında (Core_Principles §5). Kritik tipler:

```ts
// AUTH
POST /auth/login { email, password }
  → data: { accessToken, refreshToken, user: { id, firstName, lastName, role, expertise?: string[] } }
  423 → error.code "AUTH_423_ACCOUNT_LOCKED", error.details: ["remainingSeconds:540"]
POST /auth/otp/request { gsmNumber }        POST /auth/otp/verify { gsmNumber, otpCode } → login ile aynı data
POST /auth/refresh { refreshToken } → yeni çift (rotation!)

// ABONE
GET /subscribers/{id}/offers → data: OfferDto[]
OfferDto { id, campaignId, campaignNumber, title, type: "EK_PAKET"|"TARIFE_YUKSELTME"|"CIHAZ_FIRSATI"|"SADAKAT",
           discountRate, recommendationScore, isPriority, status: "SUNULDU"|"KABUL"|"RET",
           validUntil, canRate: boolean, myRating?: number }
POST /offers/{id}/respond { response: "KABUL"|"RET" }
POST /offers/{id}/rate { stars: 1|2|3|4|5 }   // ikinci kez → 409

// UZMAN / SÜPERVİZÖR
GET /cases?assignedToMe=true&status=&priority=&page= → sayfalı CaseDto[]
CaseDto { id, caseNumber, campaignTitle, segment: "YUKSEK_DEGER"|"RISKLI_KAYIP"|"YENI_ABONE"|"PASIF"|"BELIRSIZ",
          priority: "DUSUK"|"ORTA"|"YUKSEK"|"KRITIK", status: "YENI"|"ATANDI"|"OPTIMIZE_EDILIYOR"|"TEST_EDILIYOR"|"TAMAMLANDI"|"YAYINDA"|"ARSIVLENDI",
          assignedExpertId, assignedExpertName, conversionProbability, remainingSlaSeconds, slaBreached,
          expertNote, createdAt, allowedTransitions: string[] }   // ← butonları bu diziden çiz!
PATCH /cases/{id}/status { targetStatus, note? }   // kural dışı → 422, rol dışı → 403
PATCH /cases/{id}/segment { segment }              // AI override
POST  /cases/{id}/assign { expertId }              // süpervizör manuel

// DASHBOARD (süpervizör)
GET /dashboard/summary → data: {
  segmentDistribution: { segment, count }[],        // pasta grafik
  conversionTrend: { date, rate }[],                // çizgi grafik
  slaComplianceRate: number, slaBreachedActiveCases: CaseDto[],
  aiAccuracy: { overall: number, byCategory: { segment, accuracy, total }[] },   // +3 bonus ekranı
  expertPerformance: { expertId, name, completedCount, avgLift, avgDurationMinutes }[],
  pendingQueue: CaseDto[] }                          // BELIRSIZ + kapasite bekleyenler

// GAMIFICATION
GET /game/leaderboard?period=daily|weekly → data: { rank, expertId, name, points, level }[]  // ilk 10
GET /game/profile/{id} → data: { totalPoints, level: "BRONZ"|"GUMUS"|"ALTIN"|"PLATIN",
  badges: { code, name, earnedAt }[], dailyRank, weeklyRank, solvedCaseCount, avgRating }

// ADMIN
POST /users { firstName, lastName, email, password, role, expertiseAreas: string[], region }
GET /audit-logs?page=&actionType= → sayfalı { userId, userName, actionType, occurredAt, ipAddress, success, resourceId }
```

## 3. Axios Interceptor (kimlik akışının kalbi)

- [ ] Request: `Authorization: Bearer <accessToken>` ekle.
- [ ] Response 401: kuyruğa al → `/auth/refresh` çağır → yeni çifti kaydet → bekleyen istekleri yeni token'la tekrarla. Refresh de 401 dönerse → store temizle → `/login` (tüm oturumlar kapatılmış olabilir — token theft senaryosu).
- [ ] 403 → "Bu işlem için yetkiniz yok" toast; 429 → "Çok fazla deneme, lütfen bekleyin"; 423 → kalan süreyi mm:ss sayaçla göster.

## 4. Ekran-Ekran Görevler

**LoginPage:** iki sekme (Abone: GSM→OTP "1234" ipucu ekranda; Personel: e-posta+şifre). Şifre hatalarında API'nin `error.details` listesini madde madde göster (hangi kural ihlal edildi — case şartı). Kilitli hesapta geri sayım.

**OffersPage (abone):** `isPriority=true` teklifler üstte "⭐ Size Özel" rozetiyle. Kart: kampanya tipi ikonu, indirim, skor göstergesi. Kabul/İlgilenmiyorum butonları → optimistic update. Yanıt sonrası `canRate` ise `StarRating` aç; 1-2★ seçiminde "Bu teklif neden alakasızdı?" mikro-metni (UX puanı). İkinci puanlama denemesinde 409 → "Zaten puanladınız".

**MyCasesPage (uzman):** öncelik sıralı liste (KRITIK üstte, kırmızı; YUKSEK turuncu). Her kartta `SlaCountdown` (remainingSlaSeconds'tan canlı geri sayım; negatifse "SLA AŞILDI" kırmızı). Filtre: durum/öncelik.

**CaseDetailPage:** AI bilgi kutusu (segment + dönüşüm tahmini + "AI ataması" ibaresi), segment düzeltme dropdown'u (değiştirince "AI doğruluğuna işlendi" bilgisi), durum akış şeridi (state machine görseli), **butonlar SADECE `allowedTransitions`'tan render edilir** — yine de 422 gelirse toast. TAMAMLANDI'ya geçerken not modalı (boş bırakılamaz — zorunlu alan).

**DashboardPage (süpervizör):** Recharts — pasta (segment dağılımı), çizgi (dönüşüm trendi), bar (uzman performansı); AI doğruluk kartı + kategori kırılım tablosu (+3 bonus görünürlüğü); SLA aşan vakalar kırmızı liste en üstte; bekleyen kuyruk → satırda "Ata" butonu → uzman seçim modalı.

**GameProfilePage:** `LevelFrame` (seviyeye göre çerçeve rengi), rozet vitrini (kazanılmamışlar gri + koşul tooltip'i), günlük/haftalık sıralama, puan geçmişi.

**StaffPage (admin):** personel formu (expertise çoklu seçim, bölge), oluşturunca geçici şifre gösterimi. **AuditLogsPage:** filtreli tablo.

## 5. SignalR (+2 bonus)

- [ ] `useGameHub()` hook: `http://localhost:8080/hubs/game` bağlantısı (accessToken ile).
- [ ] `badge.earned` → `BadgeToast` (konfeti/animasyonlu modal — case: "rozet anında görsel bildirim" ZORUNLU).
- [ ] `points.updated` → leaderboard query invalidate (`queryClient.invalidateQueries(['leaderboard'])`) → tablo canlı güncellenir.
- [ ] Bağlantı koparsa sessizce yeniden dene; SignalR yoksa sayfa yenilemede güncel (case bunu da kabul ediyor — degrade et, kırılma).

## 6. UI/UX Puan Listesi (10 puanlık kategori — bilinçli hedefle)

- [ ] **Her** TanStack Query ekranında üç durum: `Spinner` (loading), `ErrorState` (retry butonlu), `EmptyState` (açıklayıcı metin + illüstrasyon). Bu üçlü eksikse PR merge edilmez.
- [ ] Responsive: dashboard masaüstü öncelikli; abone ekranları mobil öncelikli (Tailwind breakpoint'leri).
- [ ] Tutarlılık: renkler tek yerden — `tailwind.config` theme: primary (Turkcell sarısı #FFC900), lacivert #00457C; segment ve öncelik renkleri `SegmentBadge`/`PriorityBadge` içinde sabit.
- [ ] Form UX: zod ile anlık doğrulama, submit'te buton disabled + spinner, hata alan odaklama.
- [ ] Türkçe dil bütünlüğü: tüm metinler `src/i18n/tr.ts` sabitlerinden (dağınık string yasak).

## 7. Mock → Gerçek Geçiş Planı

1. Gün 1: MSW handler'ları bu dosyadaki sözleşmeye göre yaz; fixture'lar İskender'in seed isimleriyle aynı olsun (demo tutarlılığı).
2. Her servis Mali tarafında hazır oldukça `VITE_USE_MOCKS=false` ile o modülü gerçek API'de dene; fark çıkarsa **sözleşme dosyasında** karar verilir (kod kimin tarafındaysa o düzeltir).
3. Entegrasyon sırası: auth → offers → cases → dashboard → game/SignalR.
4. Son gün: mock kodu tree-shake dışı kalsın (prod build'e MSW girmesin).

## 8. Teslim Kontrol Listesi

- [ ] 4 rolün akışı da tıklanabilir uçtan uca (case §11.1 ve §11.2 akışları birebir)
- [ ] Zorunlu demo senaryosundaki tüm ekran anları hazır: kampanya oluşturma formu (uzman), AI skor gösterimi, atama görünümü, tamamlama modalı, liderlik tablosu canlı güncelleme
- [ ] AI kapalıyken (BELIRSIZ vaka) ekranın bozulmadığı, "AI değerlendirmesi bekleniyor — manuel kuyruğa alındı" rozetinin göründüğü doğrulandı
- [ ] Console'da error yok, build uyarısız, Lighthouse'ta bariz kırmızı yok
