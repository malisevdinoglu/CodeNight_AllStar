import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from '../api'
import type { CreateStaffRequest } from '../api/types'

export function useCreateStaff() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateStaffRequest) => adminApi.createStaff(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['audit-logs'] })
    },
  })
}

export function useAuditLogs(query: { page?: number; pageSize?: number; actionType?: string }) {
  return useQuery({
    queryKey: ['audit-logs', query],
    queryFn: () => adminApi.getAuditLogs(query),
  })
}
