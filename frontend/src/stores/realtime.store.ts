import { create } from 'zustand'

export type RealtimeStatus = 'idle' | 'connecting' | 'connected' | 'reconnecting' | 'offline' | 'mock'

type RealtimeState = {
  gameHubStatus: RealtimeStatus
  setGameHubStatus: (status: RealtimeStatus) => void
}

export const useRealtimeStore = create<RealtimeState>((set) => ({
  gameHubStatus: 'idle',
  setGameHubStatus: (status) => set({ gameHubStatus: status }),
}))
