import {
  BadgePercent,
  BarChart3,
  ClipboardList,
  FileClock,
  History,
  ListChecks,
  LogOut,
  PlusCircle,
  Rows3,
  Trophy,
  Users,
} from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import type { UserRole } from '../../api/types'
import { tr } from '../../i18n/tr'
import { useAuthStore } from '../../stores/auth.store'
import { useRealtimeStore } from '../../stores/realtime.store'
import { Badge } from '../ui'
import { GameHubBridge } from '../realtime/GameHubBridge'

type ShellNavItem = {
  label: string
  path: string
  status: string
  roles: UserRole[]
  icon: typeof BadgePercent
}

const shellNavItems: ShellNavItem[] = [
  {
    label: tr.nav.offers,
    path: '/offers',
    status: tr.phase.backendTbd,
    roles: ['MUSTERI'],
    icon: BadgePercent,
  },
  {
    label: 'Kampanyalarım',
    path: '/my-campaigns',
    status: tr.phase.backendTbd,
    roles: ['MUSTERI'],
    icon: History,
  },
  {
    label: tr.nav.cases,
    path: '/cases',
    status: tr.phase.backendTbd,
    roles: ['PERSONEL'],
    icon: ClipboardList,
  },
  {
    label: 'Yeni kampanya',
    path: '/campaigns/new',
    status: tr.phase.backendTbd,
    roles: ['PERSONEL'],
    icon: PlusCircle,
  },
  {
    label: 'Oyun profili',
    path: '/game-profile',
    status: tr.phase.backendTbd,
    roles: ['PERSONEL'],
    icon: Trophy,
  },
  {
    label: tr.nav.dashboard,
    path: '/dashboard',
    status: tr.phase.dbTbd,
    roles: ['SUPERVIZOR'],
    icon: BarChart3,
  },
  {
    label: 'Atama kuyrugu',
    path: '/queue',
    status: tr.phase.backendTbd,
    roles: ['SUPERVIZOR'],
    icon: Rows3,
  },
  {
    label: 'Tüm vakalar',
    path: '/supervisor/cases',
    status: tr.phase.backendTbd,
    roles: ['SUPERVIZOR'],
    icon: ListChecks,
  },
  {
    label: tr.nav.admin,
    path: '/admin/staff',
    status: tr.phase.backendTbd,
    roles: ['ADMIN'],
    icon: Users,
  },
  {
    label: 'Audit log',
    path: '/admin/audit-logs',
    status: tr.phase.backendTbd,
    roles: ['ADMIN'],
    icon: FileClock,
  },
]

export function AppShell() {
  const navigate = useNavigate()
  const user = useAuthStore((state) => state.user)
  const role = useAuthStore((state) => state.role)
  const clearSession = useAuthStore((state) => state.clearSession)
  const gameHubStatus = useRealtimeStore((state) => state.gameHubStatus)
  const visibleItems = role ? shellNavItems.filter((item) => item.roles.includes(role)) : []

  const handleLogout = () => {
    clearSession()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen bg-[#eef4ff] text-slate-950">
      <GameHubBridge />
      <header className="bg-[#294b98] text-white shadow-lg shadow-blue-950/10">
        <div className="mx-auto flex min-h-16 max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <img className="h-10 w-auto object-contain" src="/turkcell-logo.jpeg" alt={tr.appName} />
            <div>
              <p className="text-base font-semibold leading-5 text-white">{tr.appName}</p>
              <p className="text-xs font-medium text-white/70">{tr.appSubtitle}</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="hidden rounded-md border border-white/15 bg-white/10 px-3 py-2 text-xs font-semibold text-white/80 md:block">
              Güvenli oturum
            </div>
            <button
              className="flex size-10 items-center justify-center rounded-md border border-white/20 text-white/80 transition hover:border-brand-yellow hover:bg-white/10 hover:text-white"
              onClick={handleLogout}
              title="Çıkış yap"
              type="button"
            >
              <LogOut size={18} aria-hidden="true" />
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[17rem_1fr] lg:px-8">
        <aside className="rounded-md border border-white/80 bg-white/95 p-3 shadow-xl shadow-blue-950/10 backdrop-blur">
          <div className="mb-4 rounded-md bg-[#294b98] p-4 text-white">
            <p className="text-sm font-bold">
              {user ? `${user.firstName} ${user.lastName}` : 'Kullanıcı'}
            </p>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <p className="text-xs font-semibold uppercase text-white/70">{role}</p>
              {role === 'PERSONEL' ? (
                <Badge tone={gameHubStatus === 'connected' ? 'success' : 'neutral'}>
                  Canlı bağlantı
                </Badge>
              ) : null}
            </div>
          </div>
          <div className="mb-3 flex items-center gap-2 px-2 text-xs font-bold uppercase text-slate-500">
            <BarChart3 size={16} aria-hidden="true" />
            Menu
          </div>
          <nav className="grid gap-2">
            {visibleItems.map((item) => {
              const Icon = item.icon

              return (
                <NavLink
                  className={({ isActive }) =>
                    `rounded-md border px-3 py-3 transition ${
                      isActive
                        ? 'border-brand-yellow bg-brand-yellow text-brand-navy shadow-sm'
                        : 'border-transparent text-slate-700 hover:border-blue-100 hover:bg-blue-50 hover:text-brand-navy'
                    }`
                  }
                  key={item.label}
                  to={item.path}
                >
                  <span className="flex items-center gap-2 text-sm font-semibold">
                    <Icon size={16} aria-hidden="true" />
                    {item.label}
                  </span>
                </NavLink>
              )
            })}
          </nav>
        </aside>

        <main className="min-w-0">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
