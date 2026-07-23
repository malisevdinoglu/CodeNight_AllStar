export type UserRole = 'MUSTERI' | 'PERSONEL' | 'SUPERVIZOR' | 'ADMIN'

export type CampaignType = 'EK_PAKET' | 'TARIFE_YUKSELTME' | 'CIHAZ_FIRSATI' | 'SADAKAT'

export type OfferStatus = 'SUNULDU' | 'KABUL' | 'RET'

export type Segment = 'YUKSEK_DEGER' | 'RISKLI_KAYIP' | 'YENI_ABONE' | 'PASIF' | 'BELIRSIZ'

export type Priority = 'DUSUK' | 'ORTA' | 'YUKSEK' | 'KRITIK'

export type CaseStatus =
  | 'YENI'
  | 'ATANDI'
  | 'OPTIMIZE_EDILIYOR'
  | 'TEST_EDILIYOR'
  | 'TAMAMLANDI'
  | 'YAYINDA'
  | 'ARSIVLENDI'

export type Level = 'BRONZ' | 'GUMUS' | 'ALTIN' | 'PLATIN'

export type ApiError = {
  code: string
  message: string
  details?: string[]
}

export type ApiResponse<TData> = {
  success: boolean
  data: TData | null
  error: ApiError | null
}

export type AuthUser = {
  id: string
  firstName: string
  lastName: string
  role: UserRole
  expertise?: string[]
}

export type AuthSession = {
  accessToken: string
  refreshToken: string
  user: AuthUser
}

export type LoginRequest = {
  email: string
  password: string
}

export type OtpRequest = {
  gsmNumber: string
}

export type OtpVerifyRequest = {
  gsmNumber: string
  otpCode: string
}

export type RefreshTokenRequest = {
  refreshToken: string
}

export type OfferDto = {
  id: string
  campaignId: string
  campaignNumber: string
  title: string
  type: CampaignType
  discountRate: number
  recommendationScore: number
  isPriority: boolean
  status: OfferStatus
  validUntil: string
  canRate: boolean
  myRating?: number
}

export type OfferResponseRequest = {
  response: 'KABUL' | 'RET'
}

export type OfferRateRequest = {
  stars: 1 | 2 | 3 | 4 | 5
}

export type CaseDto = {
  id: string
  caseNumber: string
  campaignTitle: string
  segment: Segment
  priority: Priority
  status: CaseStatus
  assignedExpertId: string | null
  assignedExpertName: string | null
  conversionProbability: number | null
  remainingSlaSeconds: number
  slaBreached: boolean
  expertNote: string | null
  createdAt: string
  allowedTransitions: CaseStatus[]
}

export type CreateCampaignRequest = {
  title: string
  type: CampaignType
  targetSegment: Segment
  description: string
}

export type CreateCampaignResult = {
  campaignId: string
  campaignNumber: string
  caseId: string
  caseNumber: string
  predictedSegment: Segment
  priority: Priority
  conversionProbability: number | null
  aiAvailable: boolean
}

export type CasesQuery = {
  assignedToMe?: boolean
  status?: CaseStatus
  priority?: Priority
  page?: number
  pageSize?: number
}

export type PagedResult<TItem> = {
  items: TItem[]
  page: number
  pageSize: number
  totalCount: number
}

export type CaseStatusRequest = {
  targetStatus: CaseStatus
  note?: string
}

export type SegmentOverrideRequest = {
  segment: Segment
}

export type AssignCaseRequest = {
  expertId: string
}

export type DashboardSummaryDto = {
  segmentDistribution: { segment: Segment; count: number }[]
  conversionTrend: { date: string; rate: number }[]
  slaComplianceRate: number
  slaBreachedActiveCases: CaseDto[]
  aiAccuracy: {
    overall: number
    byCategory: { segment: Segment; accuracy: number; total: number }[]
  }
  expertPerformance: {
    expertId: string
    name: string
    completedCount: number
    avgLift: number
    avgDurationMinutes: number
  }[]
  pendingQueue: CaseDto[]
}

export type LeaderboardEntryDto = {
  rank: number
  expertId: string
  displayName: string
  points: number
  level: Level
}

// Gercek backend yaniti (GET /game/me/profile, Gamification.Application.Dtos) ile birebir -
// onceden burada var olmayan alanlar (avgRating, dailyRank, solvedCaseCount) tanimliydi ve
// GameProfilePage'de "undefined.toFixed()" crash'ine yol aciyordu.
export type GameProfileDto = {
  expertId: string
  displayName: string
  totalPoints: number
  level: Level
  completedCaseCount: number
  fastCompletionCount: number
  targetExceededCount: number
  riskliKayipSavedCount: number
  weeklyRank: number | null
  allTimeRank: number | null
  badges: { code: string; name: string; description: string; earnedAt: string | null }[]
}

export type CreateStaffRequest = {
  firstName: string
  lastName: string
  email: string
  password: string
  role: UserRole
  expertiseAreas: Segment[]
  region: string
}

export type CreateStaffResult = {
  user: AuthUser
  temporaryPassword: string
}

export type AuditLogDto = {
  userId: string
  userName: string
  actionType: string
  occurredAt: string
  ipAddress: string
  success: boolean
  resourceId: string
}
