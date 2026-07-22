import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../api'

export function useMockReadiness() {
  return useQuery({
    queryKey: ['mock-readiness'],
    queryFn: () => dashboardApi.getSummary(),
  })
}
