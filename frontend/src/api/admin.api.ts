import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type {
  ApiResponse,
  AuditLogDto,
  CreateStaffRequest,
  CreateStaffResult,
  PagedResult,
} from './types'

export const adminApi = {
  async createStaff(payload: CreateStaffRequest) {
    const response = await apiClient.post<ApiResponse<CreateStaffResult>>('/users', payload)
    return unwrapApiResponse(response.data)
  },

  async getAuditLogs(query: { page?: number; pageSize?: number; actionType?: string } = {}) {
    const response = await apiClient.get<ApiResponse<PagedResult<AuditLogDto>>>('/audit-logs', {
      params: query,
    })
    return unwrapApiResponse(response.data)
  },
}
