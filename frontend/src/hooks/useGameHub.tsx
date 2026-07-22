import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import toast from 'react-hot-toast'
import { appConfig } from '../app/config'
import { BadgeToast } from '../components/domain'
import { useAuthStore } from '../stores/auth.store'
import { useRealtimeStore } from '../stores/realtime.store'

type BadgeEarnedEvent = {
  expert_id: string
  badge_code: string
  badge_name: string
}

type PointsUpdatedEvent = {
  expert_id: string
  delta: number
  total_points: number
  reason: string
}

export function useGameHub() {
  const queryClient = useQueryClient()
  const accessToken = useAuthStore((state) => state.accessToken)
  const role = useAuthStore((state) => state.role)
  const user = useAuthStore((state) => state.user)
  const setGameHubStatus = useRealtimeStore((state) => state.setGameHubStatus)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    if (role !== 'PERSONEL' || !accessToken || !user) {
      setGameHubStatus('idle')
      return
    }

    if (appConfig.useMocks) {
      setGameHubStatus('mock')
      return
    }

    let disposed = false
    setGameHubStatus('connecting')

    import('@microsoft/signalr')
      .then(({ HubConnectionBuilder, LogLevel }) => {
        if (disposed) {
          return
        }

        const connection = new HubConnectionBuilder()
          .withUrl(appConfig.gameHubUrl, {
            accessTokenFactory: () => accessToken,
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000])
          .configureLogging(LogLevel.None)
          .build()

        connectionRef.current = connection

        connection.on('badge.earned', (payload: BadgeEarnedEvent) => {
          if (payload.expert_id !== user.id) {
            return
          }

          toast.custom(
            <div className="rounded-md border border-slate-200 bg-white p-4 shadow-panel">
              <BadgeToast badgeCode={payload.badge_code} badgeName={payload.badge_name} />
            </div>,
          )
          queryClient.invalidateQueries({ queryKey: ['game-profile', user.id] })
        })

        connection.on('points.updated', (payload: PointsUpdatedEvent) => {
          if (payload.expert_id !== user.id) {
            return
          }

          queryClient.invalidateQueries({ queryKey: ['leaderboard'] })
          queryClient.invalidateQueries({ queryKey: ['game-profile', user.id] })
          toast.success(`Puan guncellendi: ${payload.delta > 0 ? '+' : ''}${payload.delta}`)
        })

        connection.onreconnecting(() => {
          if (!disposed) {
            setGameHubStatus('reconnecting')
          }
        })

        connection.onreconnected(() => {
          if (!disposed) {
            setGameHubStatus('connected')
          }
        })

        connection.onclose(() => {
          if (!disposed) {
            setGameHubStatus('offline')
          }
        })

        connection
          .start()
          .then(() => {
            if (!disposed) {
              setGameHubStatus('connected')
            }
          })
          .catch(() => {
            if (!disposed) {
              setGameHubStatus('offline')
            }
          })
      })
      .catch(() => {
        if (!disposed) {
          setGameHubStatus('offline')
        }
      })

    return () => {
      disposed = true
      setGameHubStatus('idle')

      connectionRef.current?.stop().catch(() => undefined)
      connectionRef.current = null
    }
  }, [accessToken, queryClient, role, setGameHubStatus, user])
}
