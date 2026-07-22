import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type { ApiResponse, CreateCampaignRequest, CreateCampaignResult } from './types'

export const campaignApi = {
  async createCampaign(payload: CreateCampaignRequest) {
    const response = await apiClient.post<ApiResponse<CreateCampaignResult>>(
      '/campaigns',
      payload,
    )
    return unwrapApiResponse(response.data)
  },
}
