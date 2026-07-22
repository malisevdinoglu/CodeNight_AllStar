import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type {
  ApiResponse,
  AssignCaseRequest,
  CaseDto,
  CasesQuery,
  CaseStatusRequest,
  PagedResult,
  SegmentOverrideRequest,
} from './types'

export const caseApi = {
  async getCases(query: CasesQuery = {}) {
    const response = await apiClient.get<ApiResponse<PagedResult<CaseDto>>>('/cases', {
      params: query,
    })
    return unwrapApiResponse(response.data)
  },

  async getCase(caseId: string) {
    const response = await apiClient.get<ApiResponse<CaseDto>>(`/cases/${caseId}`)
    return unwrapApiResponse(response.data)
  },

  async updateStatus(caseId: string, payload: CaseStatusRequest) {
    const response = await apiClient.patch<ApiResponse<CaseDto>>(
      `/cases/${caseId}/status`,
      payload,
    )
    return unwrapApiResponse(response.data)
  },

  async overrideSegment(caseId: string, payload: SegmentOverrideRequest) {
    const response = await apiClient.patch<ApiResponse<CaseDto>>(
      `/cases/${caseId}/segment`,
      payload,
    )
    return unwrapApiResponse(response.data)
  },

  async assignCase(caseId: string, payload: AssignCaseRequest) {
    const response = await apiClient.post<ApiResponse<CaseDto>>(`/cases/${caseId}/assign`, payload)
    return unwrapApiResponse(response.data)
  },
}
