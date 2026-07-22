import {
  Activity,
  BadgePercent,
  BarChart3,
  ClipboardList,
  FileClock,
  History,
  ListChecks,
  LogOut,
  PlusCircle,
  Rows3,
  ShieldCheck,
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
    label: 'Kampanyalarim',
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
    // MUSTERI YAPAMAZ, digerleri (PERSONEL/SUPERVIZOR/ADMIN) yapabilir (case §3.3 netlestirme).
    roles: ['PERSONEL', 'SUPERVIZOR', 'ADMIN'],
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
    label: 'Tum vakalar',
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
    <div className="min-h-screen bg-slate-50 text-slate-950">
      <GameHubBridge />
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex min-h-16 max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-md bg-brand-yellow text-brand-navy shadow-sm">
              <Activity size={22} strokeWidth={2.4} aria-hidden="true" />
            </div>
            <div>
              <p className="text-base font-semibold leading-5 text-brand-navy">{tr.appName}</p>
              <p className="text-xs font-medium text-slate-500">{tr.appSubtitle}</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="hidden items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 md:flex">
              <ShieldCheck size={16} aria-hidden="true" />
              Core Principles uyumlu
            </div>
            <button
              className="flex size-10 items-center justify-center rounded-md border border-slate-200 text-slate-600 transition hover:border-brand-navy hover:text-brand-navy"
              onClick={handleLogout}
              title="Cikis yap"
              type="button"
            >
              <LogOut size={18} aria-hidden="true" />
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[16rem_1fr] lg:px-8">
        <aside className="rounded-md border border-slate-200 bg-white p-3 shadow-sm">
          <div className="mb-4 rounded-md bg-slate-50 p-3">
            <p className="text-sm font-bold text-slate-950">
              {user ? `${user.firstName} ${user.lastName}` : 'Kullanici'}
            </p>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <p className="text-xs font-semibold uppercase text-slate-500">{role}</p>
              {role === 'PERSONEL' ? (
                <Badge tone={gameHubStatus === 'connected' ? 'success' : 'neutral'}>
                  Hub: {gameHubStatus}
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
                    `rounded-md border px-3 py-2 transition ${
                      isActive
                        ? 'border-brand-yellow bg-brand-yellow/20 text-brand-navy'
                        : 'border-slate-200 text-slate-700 hover:border-brand-navy/40 hover:bg-slate-50'
                    }`
                  }
                  key={item.label}
                  to={item.path}
                >
                  <span className="flex items-center gap-2 text-sm font-semibold">
                    <Icon size={16} aria-hidden="true" />
                    {item.label}
                  </span>
                  <span className="mt-1 block text-xs text-slate-500">{item.status}</span>
                </NavLink>
              )
            })}
            <NavLink
              className={({ isActive }) =>
                `rounded-md border px-3 py-2 transition ${
                  isActive
                    ? 'border-brand-yellow bg-brand-yellow/20 text-brand-navy'
                    : 'border-slate-200 text-slate-700 hover:border-brand-navy/40 hover:bg-slate-50'
                }`
              }
              to="/phase-one"
            >
              <span className="flex items-center gap-2 text-sm font-semibold">
                <Activity size={16} aria-hidden="true" />
                Faz kontrolu
              </span>
              <span className="mt-1 block text-xs text-slate-500">{tr.phase.stable}</span>
            </NavLink>
          </nav>
        </aside>

        <main>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
