import type { LucideIcon } from 'lucide-react'

type PlaceholderPageProps = {
  eyebrow: string
  title: string
  description: string
  icon: LucideIcon
}

export function PlaceholderPage({
  eyebrow,
  title,
  description,
  icon: Icon,
}: PlaceholderPageProps) {
  return (
    <section className="rounded-md border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="flex flex-col gap-5 md:flex-row md:items-start md:justify-between">
        <div className="max-w-2xl">
          <p className="text-sm font-bold uppercase text-brand-navy">{eyebrow}</p>
          <h1 className="mt-2 text-2xl font-bold text-slate-950 sm:text-3xl">{title}</h1>
          <p className="mt-3 text-sm leading-6 text-slate-600">{description}</p>
        </div>
        <div className="flex size-12 shrink-0 items-center justify-center rounded-md bg-brand-yellow text-brand-navy">
          <Icon size={24} aria-hidden="true" />
        </div>
      </div>
    </section>
  )
}
