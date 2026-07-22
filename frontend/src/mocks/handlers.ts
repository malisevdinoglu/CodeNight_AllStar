import { http, HttpResponse } from 'msw'
import { appConfig } from '../app/config'
import type {
  AssignCaseRequest,
  CaseDto,
  CaseStatus,
  CreateCampaignRequest,
  CreateStaffRequest,
  OfferRateRequest,
  OfferDto,
  OfferResponseRequest,
  SegmentOverrideRequest,
} from '../api/types'
import {
  mockAuditLogs,
  mockCases,
  mockDashboard,
  mockGameProfile,
  mockLeaderboard,
  mockOffers,
  mockSessions,
  subscriberSession,
} from './fixtures'
import { fail, ok } from './utils'

const api = `${appConfig.apiUrl}/*`

function resolvePath(request: Request) {
  const apiUrl = new URL(appConfig.apiUrl)
  const requestUrl = new URL(request.url)
  return requestUrl.pathname.replace(apiUrl.pathname, '')
}

function paged<TItem>(items: TItem[], page = 1, pageSize = 20) {
  const start = (page - 1) * pageSize

  return {
    items: items.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: items.length,
  }
}

function getAllowedTransitions(status: CaseStatus): CaseStatus[] {
  const transitions: Record<CaseStatus, CaseStatus[]> = {
    YENI: ['ATANDI'],
    ATANDI: ['OPTIMIZE_EDILIYOR'],
    OPTIMIZE_EDILIYOR: ['TEST_EDILIYOR', 'TAMAMLANDI'],
    TEST_EDILIYOR: ['OPTIMIZE_EDILIYOR'],
    TAMAMLANDI: ['YAYINDA'],
    YAYINDA: ['ARSIVLENDI'],
    ARSIVLENDI: [],
  }

  return transitions[status]
}

function replaceCase(updatedCase: CaseDto) {
  const index = mockCases.findIndex((item) => item.id === updatedCase.id)

  if (index >= 0) {
    mockCases[index] = updatedCase
  }

  return updatedCase
}

function replaceOffer(updatedOffer: OfferDto) {
  const index = mockOffers.findIndex((item) => item.id === updatedOffer.id)

  if (index >= 0) {
    mockOffers[index] = updatedOffer
  }

  return updatedOffer
}

export const handlers = [
  http.post(api, async ({ request }) => {
    const path = resolvePath(request)

    if (path === '/auth/login') {
      const body = (await request.json()) as { email: string; password: string }

      if (body.email === 'locked@campaigncell.local') {
        return fail('AUTH_423_ACCOUNT_LOCKED', 'Hesap gecici olarak kilitlendi.', 423, [
          'remainingSeconds:540',
        ])
      }

      if (body.password.length < 8) {
        return fail('AUTH_400_PASSWORD_POLICY', 'Sifre politikasi ihlal edildi.', 400, [
          'En az 8 karakter olmali.',
          'En az bir buyuk harf icermeli.',
        ])
      }

      const session = mockSessions[body.email]

      if (!session) {
        return fail('AUTH_401_INVALID_CREDENTIALS', 'E-posta veya sifre hatali.', 401)
      }

      return ok(session)
    }

    if (path === '/auth/otp/request') {
      return ok({ expiresInSeconds: 180 })
    }

    if (path === '/auth/otp/verify') {
      const body = (await request.json()) as { otpCode: string }

      if (body.otpCode !== '1234') {
        return fail('AUTH_400_INVALID_OTP', 'OTP kodu hatali.', 400)
      }

      return ok(subscriberSession)
    }

    if (path === '/auth/refresh') {
      return ok({
        ...subscriberSession,
        accessToken: `mock-access-refreshed-${Date.now()}`,
      })
    }

    if (path === '/campaigns') {
      const body = (await request.json()) as CreateCampaignRequest
      const aiAvailable = body.targetSegment !== 'BELIRSIZ'
      const createdCase: CaseDto = {
        id: `case-${Date.now()}`,
        caseNumber: 'CASE-2026-000080',
        campaignTitle: body.title,
        segment: aiAvailable ? body.targetSegment : 'BELIRSIZ',
        priority: body.targetSegment === 'RISKLI_KAYIP' ? 'YUKSEK' : 'ORTA',
        status: aiAvailable ? 'ATANDI' : 'YENI',
        assignedExpertId: aiAvailable ? 'expert-001' : null,
        assignedExpertName: aiAvailable ? 'Osman Erkan' : null,
        conversionProbability: aiAvailable ? 0.76 : null,
        remainingSlaSeconds: aiAvailable ? 28_800 : 86_400,
        slaBreached: false,
        expertNote: null,
        createdAt: new Date().toISOString(),
        allowedTransitions: aiAvailable ? ['OPTIMIZE_EDILIYOR'] : [],
      }

      mockCases.unshift(createdCase)

      return ok(
        {
          campaignId: 'campaign-new',
          campaignNumber: 'CMP-2026-000123',
          caseId: createdCase.id,
          caseNumber: createdCase.caseNumber,
          predictedSegment: createdCase.segment,
          priority: createdCase.priority,
          conversionProbability: aiAvailable ? 0.76 : null,
          aiAvailable,
        },
        { status: 201 },
      )
    }

    if (path.endsWith('/assign')) {
      const caseId = path.split('/')[2]
      const body = (await request.json()) as AssignCaseRequest
      const targetCase = mockCases.find((item) => item.id === caseId)

      if (!targetCase) {
        return fail('CMP_404_CASE_NOT_FOUND', 'Vaka bulunamadi.', 404)
      }

      return ok(replaceCase({
        ...targetCase,
        status: 'ATANDI',
        assignedExpertId: body.expertId,
        assignedExpertName: 'Secilen Uzman',
        allowedTransitions: ['OPTIMIZE_EDILIYOR'],
      }))
    }

    if (path.endsWith('/respond')) {
      const offerId = path.split('/')[2]
      const body = (await request.json()) as OfferResponseRequest
      const offer = mockOffers.find((item) => item.id === offerId)

      if (!offer) {
        return fail('CMP_404_OFFER_NOT_FOUND', 'Teklif bulunamadi.', 404)
      }

      return ok(replaceOffer({ ...offer, status: body.response, canRate: true }))
    }

    if (path.endsWith('/rate')) {
      const offerId = path.split('/')[2]
      const body = (await request.json()) as OfferRateRequest
      const offer = mockOffers.find((item) => item.id === offerId)

      if (!offer) {
        return fail('CMP_404_OFFER_NOT_FOUND', 'Teklif bulunamadi.', 404)
      }

      if (offer.myRating) {
        return fail('CMP_409_ALREADY_RATED', 'Zaten puanladiniz.', 409)
      }

      return ok(replaceOffer({ ...offer, canRate: false, myRating: body.stars }))
    }

    if (path === '/users') {
      const body = (await request.json()) as CreateStaffRequest
      const createdAt = new Date().toISOString()
      const userId = `staff-${Date.now()}`
      const temporaryPassword = body.password || 'TempPass1'
      const createdUser = {
        id: userId,
        firstName: body.firstName,
        lastName: body.lastName,
        role: body.role,
        expertise: body.expertiseAreas,
      }

      mockAuditLogs.unshift({
        userId: 'admin-001',
        userName: 'Admin Kullanici',
        actionType: 'USER_CREATE',
        occurredAt: createdAt,
        ipAddress: '10.0.0.10',
        success: true,
        resourceId: userId,
      })

      return ok(
        {
          user: createdUser,
          temporaryPassword,
        },
        { status: 201 },
      )
    }

    return HttpResponse.json(null, { status: 404 })
  }),

  http.patch(api, async ({ request }) => {
    const path = resolvePath(request)

    if (path.endsWith('/status')) {
      const caseId = path.split('/')[2]
      const body = (await request.json()) as { targetStatus: CaseStatus; note?: string }
      const targetCase = mockCases.find((item) => item.id === caseId)

      if (!targetCase) {
        return fail('CMP_404_CASE_NOT_FOUND', 'Vaka bulunamadi.', 404)
      }

      if (!targetCase.allowedTransitions.includes(body.targetStatus)) {
        return fail(
          'CMP_422_INVALID_TRANSITION',
          `${targetCase.status} durumundan ${body.targetStatus} durumuna gecilemez.`,
          422,
        )
      }

      if (body.targetStatus === 'TAMAMLANDI' && !body.note?.trim()) {
        return fail('CMP_400_EXPERT_NOTE_REQUIRED', 'Tamamlama notu zorunludur.', 400)
      }

      return ok(replaceCase({
        ...targetCase,
        status: body.targetStatus,
        expertNote: body.note ?? targetCase.expertNote,
        allowedTransitions: getAllowedTransitions(body.targetStatus),
      }))
    }

    if (path.endsWith('/segment')) {
      const caseId = path.split('/')[2]
      const body = (await request.json()) as SegmentOverrideRequest
      const targetCase = mockCases.find((item) => item.id === caseId)

      if (!targetCase) {
        return fail('CMP_404_CASE_NOT_FOUND', 'Vaka bulunamadi.', 404)
      }

      return ok(replaceCase({ ...targetCase, segment: body.segment }))
    }

    return HttpResponse.json(null, { status: 404 })
  }),

  http.get(api, ({ request }) => {
    const path = resolvePath(request)
    const url = new URL(request.url)

    if (path.startsWith('/subscribers/') && path.endsWith('/offers')) {
      return ok(mockOffers)
    }

    if (path.startsWith('/offers/')) {
      const offerId = path.split('/')[2]
      const offer = mockOffers.find((item) => item.id === offerId)

      if (!offer) {
        return fail('CMP_404_OFFER_NOT_FOUND', 'Teklif bulunamadi.', 404)
      }

      return ok(offer)
    }

    if (path === '/cases') {
      const page = Number(url.searchParams.get('page') ?? '1')
      const pageSize = Number(url.searchParams.get('pageSize') ?? '20')
      const status = url.searchParams.get('status')
      const priority = url.searchParams.get('priority')
      const assignedToMe = url.searchParams.get('assignedToMe') === 'true'

      const filteredCases = mockCases.filter((item) => {
        if (assignedToMe && item.assignedExpertId !== 'expert-001') {
          return false
        }

        if (status && item.status !== status) {
          return false
        }

        if (priority && item.priority !== priority) {
          return false
        }

        return true
      })

      return ok(paged(filteredCases, page, pageSize))
    }

    if (path.startsWith('/cases/')) {
      const caseId = path.split('/')[2]
      const targetCase = mockCases.find((item) => item.id === caseId)

      if (!targetCase) {
        return fail('CMP_404_CASE_NOT_FOUND', 'Vaka bulunamadi.', 404)
      }

      return ok(targetCase)
    }

    if (path === '/dashboard/summary') {
      return ok({
        ...mockDashboard,
        slaBreachedActiveCases: mockCases.filter((item) => item.slaBreached),
        pendingQueue: mockCases.filter(
          (item) => item.segment === 'BELIRSIZ' || item.assignedExpertId === null,
        ),
      })
    }

    if (path === '/game/leaderboard') {
      return ok(mockLeaderboard)
    }

    if (path.startsWith('/game/profile/')) {
      return ok(mockGameProfile)
    }

    if (path === '/audit-logs') {
      const page = Number(url.searchParams.get('page') ?? '1')
      const pageSize = Number(url.searchParams.get('pageSize') ?? '20')
      const actionType = url.searchParams.get('actionType')
      const logs = actionType
        ? mockAuditLogs.filter((item) => item.actionType === actionType)
        : mockAuditLogs

      return ok(paged(logs, page, pageSize))
    }

    return HttpResponse.json(null, { status: 404 })
  }),
]
