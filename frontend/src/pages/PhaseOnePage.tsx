import { CheckCircle2, Database, Network, Palette } from 'lucide-react'
import { appConfig } from '../app/config'
import { tr } from '../i18n/tr'

const readinessItems = [
  {
    title: 'React 18 + Vite',
    description: 'TypeScript tabanli frontend projesi olusturuldu.',
    icon: CheckCircle2,
    status: tr.phase.stable,
  },
  {
    title: 'Tema altyapisi',
    description: 'Turkcell sari/lacivert kimligi modern operasyon paneli icin tanimlandi.',
    icon: Palette,
    status: tr.phase.stable,
  },
  {
    title: 'API siniri',
    description: 'Endpoint farklari sayfalara yayilmadan api katmaninda karsilanacak.',
    icon: Network,
    status: tr.phase.backendTbd,
  },
  {
    title: 'Mock stratejisi',
    description: 'MSW modulu backend ve DB seed netlesince sozlesmeye gore doldurulacak.',
    icon: Database,
    status: tr.phase.dbTbd,
  },
] as const

export function PhaseOnePage() {
  return (
    <section className="space-y-6">
      <div className="rounded-md border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
          <div className="max-w-2xl">
            <p className="text-sm font-bold uppercase text-brand-navy">Faz 1</p>
            <h1 className="mt-2 text-2xl font-bold text-slate-950 sm:text-3xl">
              {tr.phase.title}
            </h1>
            <p className="mt-3 text-sm leading-6 text-slate-600">{tr.phase.description}</p>
          </div>

          <div className="rounded-md border border-brand-yellow/60 bg-brand-yellow/15 px-4 py-3">
            <p className="text-xs font-bold uppercase text-brand-navy">Mock modu</p>
            <p className="mt-1 text-lg font-bold text-slate-950">
              {appConfig.useMocks ? 'Acik' : 'Kapali'}
            </p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {readinessItems.map((item) => {
          const Icon = item.icon

          return (
            <article
              className="rounded-md border border-slate-200 bg-white p-5 shadow-sm"
              key={item.title}
            >
              <div className="flex items-start gap-4">
                <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-brand-navy text-white">
                  <Icon size={20} aria-hidden="true" />
                </div>
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-base font-bold text-slate-950">{item.title}</h2>
                    <span className="rounded-sm bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-600">
                      {item.status}
                    </span>
                  </div>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{item.description}</p>
                </div>
              </div>
            </article>
          )
        })}
      </div>
    </section>
  )
}
