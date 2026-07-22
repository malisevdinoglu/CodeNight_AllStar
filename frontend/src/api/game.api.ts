import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type { ApiResponse, GameProfileDto, LeaderboardEntryDto } from './types'

export const gameApi = {
  async getLeaderboard(period: 'daily' | 'weekly') {
    const response = await apiClient.get<ApiResponse<LeaderboardEntryDto[]>>('/game/leaderboard', {
      params: { period },
    })
    return unwrapApiResponse(response.data)
  },

  async getProfile(expertId: string) {
    const response = await apiClient.get<ApiResponse<GameProfileDto>>(`/game/profile/${expertId}`)
    return unwrapApiResponse(response.data)
  },
}
