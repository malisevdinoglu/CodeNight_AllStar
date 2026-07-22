import { useMutation, useQueryClient } from '@tanstack/react-query'
import { campaignApi } from '../api'

export function useCreateCampaign() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: campaignApi.createCampaign,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cases'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
    },
  })
}
