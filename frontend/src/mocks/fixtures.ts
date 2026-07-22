import type {
  AuditLogDto,
  AuthSession,
  CaseDto,
  DashboardSummaryDto,
  GameProfileDto,
  LeaderboardEntryDto,
  OfferDto,
} from '../api/types'

const now = new Date('2026-07-22T09:00:00.000Z')

export const mockSessions: Record<string, AuthSession> = {
  'personel@campaigncell.local': {
    accessToken: 'mock-access-personel',
    refreshToken: 'mock-refresh-personel',
    user: {
      id: 'expert-001',
      firstName: 'Osman',
      lastName: 'Erkan',
      role: 'PERSONEL',
      expertise: ['RISKLI_KAYIP', 'YUKSEK_DEGER'],
    },
  },
  'supervisor@campaigncell.local': {
    accessToken: 'mock-access-supervisor',
    refreshToken: 'mock-refresh-supervisor',
    user: {
      id: 'supervisor-001',
      firstName: 'Ece',
      lastName: 'Yildiz',
      role: 'SUPERVIZOR',
    },
  },
  'admin@campaigncell.local': {
    accessToken: 'mock-access-admin',
    refreshToken: 'mock-refresh-admin',
    user: {
      id: 'admin-001',
      firstName: 'Admin',
      lastName: 'Kullanici',
      role: 'ADMIN',
    },
  },
}

export const subscriberSession: AuthSession = {
  accessToken: 'mock-access-musteri',
  refreshToken: 'mock-refresh-musteri',
  user: {
    id: 'subscriber-001',
    firstName: 'Deniz',
    lastName: 'Kara',
    role: 'MUSTERI',
  },
}

export const mockOffers: OfferDto[] = [
  {
    id: 'offer-001',
    campaignId: 'campaign-001',
    campaignNumber: 'CMP-2026-000121',
    title: 'Yuksek hizli ek internet paketi',
    type: 'EK_PAKET',
    discountRate: 30,
    recommendationScore: 91,
    isPriority: true,
    status: 'SUNULDU',
    validUntil: '2026-07-29T20:59:59.000Z',
    canRate: false,
  },
  {
    id: 'offer-002',
    campaignId: 'campaign-002',
    campaignNumber: 'CMP-2026-000122',
    title: 'Sadakat indirimi ve cihaz firsati',
    type: 'SADAKAT',
    discountRate: 18,
    recommendationScore: 74,
    isPriority: false,
    status: 'SUNULDU',
    validUntil: '2026-08-01T20:59:59.000Z',
    canRate: false,
  },
]

export const mockCases: CaseDto[] = [
  {
    id: 'case-001',
    caseNumber: 'CASE-2026-000077',
    campaignTitle: 'Riskli kayip aboneleri geri kazanma',
    segment: 'RISKLI_KAYIP',
    priority: 'KRITIK',
    status: 'ATANDI',
    assignedExpertId: 'expert-001',
    assignedExpertName: 'Osman Erkan',
    conversionProbability: 0.82,
    remainingSlaSeconds: 4380,
    slaBreached: false,
    expertNote: null,
    createdAt: now.toISOString(),
    allowedTransitions: ['OPTIMIZE_EDILIYOR'],
  },
  {
    id: 'case-002',
    caseNumber: 'CASE-2026-000078',
    campaignTitle: 'Yuksek deger segmentine cihaz teklifi',
    segment: 'YUKSEK_DEGER',
    priority: 'YUKSEK',
    status: 'OPTIMIZE_EDILIYOR',
    assignedExpertId: 'expert-001',
    assignedExpertName: 'Osman Erkan',
    conversionProbability: 0.67,
    remainingSlaSeconds: 15_600,
    slaBreached: false,
    expertNote: null,
    createdAt: '2026-07-22T07:15:00.000Z',
    allowedTransitions: ['TEST_EDILIYOR', 'TAMAMLANDI'],
  },
  {
    id: 'case-003',
    caseNumber: 'CASE-2026-000079',
    campaignTitle: 'AI servis kapali fallback kampanyasi',
    segment: 'BELIRSIZ',
    priority: 'ORTA',
    status: 'YENI',
    assignedExpertId: null,
    assignedExpertName: null,
    conversionProbability: null,
    remainingSlaSeconds: 82_400,
    slaBreached: false,
    expertNote: null,
    createdAt: '2026-07-22T08:20:00.000Z',
    allowedTransitions: [],
  },
]

export const mockDashboard: DashboardSummaryDto = {
  segmentDistribution: [
    { segment: 'RISKLI_KAYIP', count: 18 },
    { segment: 'YUKSEK_DEGER', count: 14 },
    { segment: 'YENI_ABONE', count: 9 },
    { segment: 'PASIF', count: 6 },
    { segment: 'BELIRSIZ', count: 3 },
  ],
  conversionTrend: [
    { date: '2026-07-16', rate: 18.4 },
    { date: '2026-07-17', rate: 19.1 },
    { date: '2026-07-18', rate: 20.6 },
    { date: '2026-07-19', rate: 21.8 },
    { date: '2026-07-20', rate: 22.2 },
    { date: '2026-07-21', rate: 23.9 },
    { date: '2026-07-22', rate: 24.7 },
  ],
  slaComplianceRate: 93.5,
  slaBreachedActiveCases: [],
  aiAccuracy: {
    overall: 88.4,
    byCategory: [
      { segment: 'RISKLI_KAYIP', accuracy: 91.2, total: 132 },
      { segment: 'YUKSEK_DEGER', accuracy: 86.5, total: 96 },
      { segment: 'YENI_ABONE', accuracy: 84.8, total: 73 },
    ],
  },
  expertPerformance: [
    {
      expertId: 'expert-001',
      name: 'Osman Erkan',
      completedCount: 12,
      avgLift: 16.2,
      avgDurationMinutes: 74,
    },
    {
      expertId: 'expert-002',
      name: 'Mali Demir',
      completedCount: 10,
      avgLift: 14.1,
      avgDurationMinutes: 83,
    },
  ],
  pendingQueue: [mockCases[2]],
}

export const mockLeaderboard: LeaderboardEntryDto[] = [
  { rank: 1, expertId: 'expert-001', name: 'Osman Erkan', points: 425, level: 'ALTIN' },
  { rank: 2, expertId: 'expert-002', name: 'Mali Demir', points: 390, level: 'ALTIN' },
  { rank: 3, expertId: 'expert-003', name: 'Iskender Aksoy', points: 340, level: 'GUMUS' },
]

export const mockGameProfile: GameProfileDto = {
  totalPoints: 425,
  level: 'ALTIN',
  badges: [
    { code: 'SLA_HIZLI', name: 'SLA Ustasi', earnedAt: '2026-07-21T14:20:00.000Z' },
    { code: 'KRITIK_KURTARICI', name: 'Kritik Kurtarici', earnedAt: '2026-07-22T08:45:00.000Z' },
    { code: 'MUKEMMEL_PUAN', name: 'Mukemmel Puan', earnedAt: null },
  ],
  dailyRank: 1,
  weeklyRank: 2,
  solvedCaseCount: 28,
  avgRating: 4.6,
}

export const mockAuditLogs: AuditLogDto[] = [
  {
    userId: 'supervisor-001',
    userName: 'Ece Yildiz',
    actionType: 'CASE_ASSIGN',
    occurredAt: '2026-07-22T08:30:00.000Z',
    ipAddress: '10.0.0.14',
    success: true,
    resourceId: 'case-003',
  },
  {
    userId: 'subscriber-001',
    userName: 'Deniz Kara',
    actionType: 'FORBIDDEN_DASHBOARD_ACCESS',
    occurredAt: '2026-07-22T08:42:00.000Z',
    ipAddress: '10.0.0.33',
    success: false,
    resourceId: 'dashboard',
  },
]
