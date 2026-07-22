import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { caseApi } from '../api'
import type {
  AssignCaseRequest,
  CaseStatusRequest,
  CasesQuery,
  SegmentOverrideRequest,
} from '../api/types'

export function useCases(query: CasesQuery = {}) {
  return useQuery({
    queryKey: ['cases', query],
    queryFn: () => caseApi.getCases(query),
  })
}

export function useCaseDetail(caseId: string | undefined) {
  return useQuery({
    enabled: Boolean(caseId),
    queryKey: ['case', caseId],
    queryFn: () => caseApi.getCase(caseId ?? ''),
  })
}

export function useUpdateCaseStatus(caseId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CaseStatusRequest) => caseApi.updateStatus(caseId, payload),
    onSuccess: (updatedCase) => {
      queryClient.setQueryData(['case', caseId], updatedCase)
      queryClient.invalidateQueries({ queryKey: ['cases'] })
    },
  })
}

export function useOverrideSegment(caseId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: SegmentOverrideRequest) => caseApi.overrideSegment(caseId, payload),
    onSuccess: (updatedCase) => {
      queryClient.setQueryData(['case', caseId], updatedCase)
      queryClient.invalidateQueries({ queryKey: ['cases'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
    },
  })
}

export function useAssignCase(caseId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: AssignCaseRequest) => caseApi.assignCase(caseId, payload),
    onSuccess: (updatedCase) => {
      queryClient.setQueryData(['case', caseId], updatedCase)
      queryClient.invalidateQueries({ queryKey: ['cases'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
    },
  })
}
