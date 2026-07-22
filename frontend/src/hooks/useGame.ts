import { useQuery } from '@tanstack/react-query'
import { gameApi } from '../api'

export function useGameProfile(expertId: string | undefined) {
  return useQuery({
    enabled: Boolean(expertId),
    queryKey: ['game-profile', expertId],
    queryFn: () => gameApi.getProfile(expertId ?? ''),
  })
}

export function useLeaderboard(period: 'daily' | 'weekly') {
  return useQuery({
    queryKey: ['leaderboard', period],
    queryFn: () => gameApi.getLeaderboard(period),
  })
}
