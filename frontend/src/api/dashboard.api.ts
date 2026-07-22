import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type { ApiResponse, DashboardSummaryDto } from './types'

export const dashboardApi = {
  async getSummary() {
    const response = await apiClient.get<ApiResponse<DashboardSummaryDto>>('/dashboard/summary')
    return unwrapApiResponse(response.data)
  },
}
