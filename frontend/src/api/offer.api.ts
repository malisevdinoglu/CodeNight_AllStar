import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type {
  ApiResponse,
  OfferDto,
  OfferRateRequest,
  OfferResponseRequest,
} from './types'

export const offerApi = {
  async getSubscriberOffers(subscriberId: string) {
    const response = await apiClient.get<ApiResponse<OfferDto[]>>(
      `/subscribers/${subscriberId}/offers`,
    )
    return unwrapApiResponse(response.data)
  },

  async respondToOffer(offerId: string, payload: OfferResponseRequest) {
    const response = await apiClient.post<ApiResponse<OfferDto>>(
      `/offers/${offerId}/respond`,
      payload,
    )
    return unwrapApiResponse(response.data)
  },

  async rateOffer(offerId: string, payload: OfferRateRequest) {
    const response = await apiClient.post<ApiResponse<OfferDto>>(`/offers/${offerId}/rate`, payload)
    return unwrapApiResponse(response.data)
  },
}
