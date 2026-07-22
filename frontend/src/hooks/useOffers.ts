import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { offerApi } from '../api'
import type { OfferRateRequest, OfferResponseRequest } from '../api/types'

export function useSubscriberOffers(subscriberId: string | undefined) {
  return useQuery({
    enabled: Boolean(subscriberId),
    queryKey: ['offers', subscriberId],
    queryFn: () => offerApi.getSubscriberOffers(subscriberId ?? ''),
  })
}

export function useOfferDetail(offerId: string | undefined) {
  return useQuery({
    enabled: Boolean(offerId),
    queryKey: ['offer', offerId],
    queryFn: () => offerApi.getOffer(offerId ?? ''),
  })
}

export function useRespondToOffer(subscriberId: string | undefined) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ offerId, payload }: { offerId: string; payload: OfferResponseRequest }) =>
      offerApi.respondToOffer(offerId, payload),
    onSuccess: (updatedOffer) => {
      queryClient.setQueryData(['offer', updatedOffer.id], updatedOffer)
      queryClient.invalidateQueries({ queryKey: ['offers', subscriberId] })
    },
  })
}

export function useRateOffer(subscriberId: string | undefined) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ offerId, payload }: { offerId: string; payload: OfferRateRequest }) =>
      offerApi.rateOffer(offerId, payload),
    onSuccess: (updatedOffer) => {
      queryClient.setQueryData(['offer', updatedOffer.id], updatedOffer)
      queryClient.invalidateQueries({ queryKey: ['offers', subscriberId] })
    },
  })
}
