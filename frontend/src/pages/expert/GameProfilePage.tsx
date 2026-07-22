import { Award, Trophy } from 'lucide-react'
import { BadgeToast, LeaderboardTable, LevelFrame } from '../../components/domain'
import {
  Badge,
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

export function GameProfilePage() {
  const user = useAuthStore((state) => state.user)
  const profileQuery = useGameProfile(user?.id)
  const leaderboardQuery = useLeaderboard('weekly')

  if (profileQuery.isLoading) {
    return <Spinner className="min-h-80" label="Oyun profili yukleniyor" />
  }

  if (profileQuery.isError) {
    return <ErrorState onRetry={() => profileQuery.refetch()} title="Oyun profili alinamadi" />
  }

  if (!profileQuery.data) {
    return (
      <EmptyState
        description="Gamification verisi backend geldikten sonra bu ekranda gorunecek."
        title="Profil bulunamadi"
      />
    )
  }

  return (
    <section className="grid gap-6 xl:grid-cols-[1fr_26rem]">
      <div className="space-y-6">
        <LevelFrame level={profileQuery.data.level}>
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-sm font-bold uppercase text-brand-navy">Gamification</p>
              <h1 className="mt-2 text-3xl font-bold text-slate-950">{profileQuery.data.level}</h1>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                Toplam {profileQuery.data.totalPoints} puan - {profileQuery.data.solvedCaseCount}{' '}
                vaka tamamlandi
              </p>
            </div>
            <div className="flex size-16 items-center justify-center rounded-md bg-brand-navy text-brand-yellow">
              <Trophy size={32} aria-hidden="true" />
            </div>
          </div>
        </LevelFrame>

        <Card>
          <CardHeader>
            <CardTitle>Rozet vitrini</CardTitle>
            <CardDescription>Kazanilmamis rozetler kosul tooltip'i icin hazir alanla gelir.</CardDescription>
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
                  title={badge.earnedAt ? badge.name : 'Kosul backend geldikten sonra netlesecek'}
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
        <Card>
          <CardHeader>
            <CardTitle>Siralama</CardTitle>
            <CardDescription>Gunluk ve haftalik konum.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-md border border-slate-200 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Gunluk</p>
                <p className="mt-2 text-2xl font-bold text-brand-navy">
                  #{profileQuery.data.dailyRank}
                </p>
              </div>
              <div className="rounded-md border border-slate-200 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Haftalik</p>
                <p className="mt-2 text-2xl font-bold text-brand-navy">
                  #{profileQuery.data.weeklyRank}
                </p>
              </div>
            </div>
            <div className="mt-4 rounded-md border border-slate-200 p-4">
              <p className="text-xs font-bold uppercase text-slate-500">Ortalama puanlama</p>
              <p className="mt-2 text-2xl font-bold text-slate-950">
                {profileQuery.data.avgRating.toFixed(1)}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Haftalik liderlik</CardTitle>
            <CardDescription>SignalR geldiginde canli invalidate edilecek tablo.</CardDescription>
          </CardHeader>
          <CardContent>
            {leaderboardQuery.isLoading ? <Spinner label="Liderlik yukleniyor" /> : null}
            {leaderboardQuery.isError ? (
              <ErrorState onRetry={() => leaderboardQuery.refetch()} title="Liderlik alinamadi" />
            ) : null}
            {leaderboardQuery.data ? <LeaderboardTable entries={leaderboardQuery.data} /> : null}
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <BadgeToast badgeCode="DEMO_BADGE" badgeName="Anlik rozet bildirimi onizleme" />
          </CardContent>
        </Card>
      </div>
    </section>
  )
}
