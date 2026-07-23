import { useQueryClient } from '@tanstack/react-query'
import { Award, Trophy } from 'lucide-react'
import toast from 'react-hot-toast'
import { BadgeToast, LeaderboardTable, LevelFrame } from '../../components/domain'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  Spinner,
} from '../../components/ui'
import { useGameProfile, useLeaderboard } from '../../hooks/useGame'
import { useAuthStore } from '../../stores/auth.store'
import { useRealtimeStore } from '../../stores/realtime.store'

export function GameProfilePage() {
  const queryClient = useQueryClient()
  const user = useAuthStore((state) => state.user)
  const gameHubStatus = useRealtimeStore((state) => state.gameHubStatus)
  const profileQuery = useGameProfile(user?.id)
  const leaderboardQuery = useLeaderboard('weekly')

  if (profileQuery.isLoading) {
    return <Spinner className="min-h-80" label="Oyun profili yükleniyor" />
  }

  if (profileQuery.isError) {
    return <ErrorState onRetry={() => profileQuery.refetch()} title="Oyun profili alınamadı" />
  }

  if (!profileQuery.data) {
    return (
      <EmptyState
        description="Performans bilgileri şu anda görüntülenemiyor."
        title="Profil bulunamadı"
      />
    )
  }

  const handleMockBadge = () => {
    toast.custom(
      <div className="rounded-md border border-slate-200 bg-white p-4 shadow-panel">
        <BadgeToast badgeCode="SLA_HIZLI" badgeName="SLA Ustasi" />
      </div>,
    )
    queryClient.invalidateQueries({ queryKey: ['leaderboard'] })
    queryClient.invalidateQueries({ queryKey: ['game-profile', user?.id] })
  }

  return (
    <section className="grid gap-6 xl:grid-cols-[1fr_26rem]">
      <div className="space-y-6">
        <LevelFrame level={profileQuery.data.level}>
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-sm font-bold uppercase text-brand-navy">Performans profili</p>
              <h1 className="mt-2 text-3xl font-bold text-slate-950">{profileQuery.data.level}</h1>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                Toplam {profileQuery.data.totalPoints} puan - {profileQuery.data.completedCaseCount}{' '}
                vaka tamamlandı
              </p>
            </div>
            <div className="flex size-16 items-center justify-center rounded-md bg-brand-navy text-brand-yellow">
              <Trophy size={32} aria-hidden="true" />
            </div>
          </div>
        </LevelFrame>

        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Rozet vitrini</CardTitle>
            <CardDescription>Kazandığın rozetler ve ilerleme durumun.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 md:grid-cols-3">
              {profileQuery.data.badges.map((badge) => (
                <div
                  className={`rounded-md border p-4 ${
                    badge.earnedAt
                      ? 'border-brand-yellow bg-brand-yellow/15'
                      : 'border-slate-200 bg-slate-50 opacity-70'
                   }`}
                  key={badge.code}
                  title={badge.earnedAt ? badge.name : 'Henüz kazanılmadı'}
                >
                  <Award
                    className={badge.earnedAt ? 'text-brand-navy' : 'text-slate-400'}
                    size={22}
                    aria-hidden="true"
                  />
                  <p className="mt-3 text-sm font-bold text-slate-950">{badge.name}</p>
                  <Badge className="mt-2" tone={badge.earnedAt ? 'brand' : 'neutral'}>
                    {badge.code}
                  </Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="space-y-6">
        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Sıralama</CardTitle>
            <CardDescription>Tüm zamanlar ve haftalık konum.</CardDescription>
          </CardHeader>
          <CardContent>
            {/* Backend'de avgRating alani hic yok (crash sebebiydi); dailyRank de yok, gercek
                alan allTimeRank. Rank'lar null olabilir (leaderboard'da henuz siralanmamis
                olabilir, orn. Redis cache'de yoksa) - null-safe render edildi. */}
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-md border border-blue-100 bg-blue-50/50 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Tüm zamanlar</p>
                <p className="mt-2 text-2xl font-bold text-brand-navy">
                  {profileQuery.data.allTimeRank !== null ? `#${profileQuery.data.allTimeRank}` : '—'}
                </p>
              </div>
              <div className="rounded-md border border-blue-100 bg-blue-50/50 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Haftalık</p>
                <p className="mt-2 text-2xl font-bold text-brand-navy">
                  {profileQuery.data.weeklyRank !== null ? `#${profileQuery.data.weeklyRank}` : '—'}
                </p>
              </div>
            </div>
            <div className="mt-4 grid grid-cols-2 gap-3">
              <div className="rounded-md border border-blue-100 bg-blue-50/50 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Hızlı tamamlama</p>
                <p className="mt-2 text-2xl font-bold text-slate-950">
                  {profileQuery.data.fastCompletionCount}
                </p>
              </div>
              <div className="rounded-md border border-blue-100 bg-blue-50/50 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Riskli kayıp kurtarma</p>
                <p className="mt-2 text-2xl font-bold text-slate-950">
                  {profileQuery.data.riskliKayipSavedCount}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Anlık Bildirimler</CardTitle>
            <CardDescription>Rozet ve başarı bildirimlerini buradan takip edebilirsin.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <Badge tone={gameHubStatus === 'connected' ? 'success' : 'neutral'}>
                Bildirimler: {gameHubStatus === 'connected' ? 'açık' : 'beklemede'}
              </Badge>
              <Button onClick={handleMockBadge} variant="secondary">
                Rozet Bildirimi Göster
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Haftalık Liderlik</CardTitle>
            <CardDescription>Bu haftaki ekip sıralaması.</CardDescription>
          </CardHeader>
          <CardContent>
            {leaderboardQuery.isLoading ? <Spinner label="Liderlik yükleniyor" /> : null}
            {leaderboardQuery.isError ? (
              <ErrorState onRetry={() => leaderboardQuery.refetch()} title="Liderlik alınamadı" />
            ) : null}
            {leaderboardQuery.data ? <LeaderboardTable entries={leaderboardQuery.data} /> : null}
          </CardContent>
        </Card>

      </div>
    </section>
  )
}
