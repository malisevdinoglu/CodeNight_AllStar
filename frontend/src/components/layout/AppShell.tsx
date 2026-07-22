import { Activity, BarChart3, ShieldCheck } from 'lucide-react'
import { Outlet } from 'react-router-dom'
import { tr } from '../../i18n/tr'

const shellNavItems = [
  { label: tr.nav.offers, status: tr.phase.backendTbd },
  { label: tr.nav.cases, status: tr.phase.backendTbd },
  { label: tr.nav.dashboard, status: tr.phase.dbTbd },
  { label: tr.nav.admin, status: tr.phase.backendTbd },
] as const

export function AppShell() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-950">
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

          <div className="hidden items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 md:flex">
            <ShieldCheck size={16} aria-hidden="true" />
            Core Principles uyumlu
          </div>
        </div>
      </header>

      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[16rem_1fr] lg:px-8">
        <aside className="rounded-md border border-slate-200 bg-white p-3 shadow-sm">
          <div className="mb-3 flex items-center gap-2 px-2 text-xs font-bold uppercase text-slate-500">
            <BarChart3 size={16} aria-hidden="true" />
            Faz 1 navigasyon
          </div>
          <nav className="grid gap-2">
            {shellNavItems.map((item) => (
              <div
                className="rounded-md border border-slate-200 px-3 py-2"
                key={item.label}
              >
                <p className="text-sm font-semibold text-slate-800">{item.label}</p>
                <p className="mt-1 text-xs text-slate-500">{item.status}</p>
              </div>
            ))}
          </nav>
        </aside>

        <main>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
