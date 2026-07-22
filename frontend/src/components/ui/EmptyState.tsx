import type { LucideIcon } from 'lucide-react'
import { Inbox } from 'lucide-react'
import type { ReactNode } from 'react'
import { classNames } from '../../lib/classNames'

type EmptyStateProps = {
  title: string
  description: string
  icon?: LucideIcon
  action?: ReactNode
  className?: string
}

export function EmptyState({
  action,
  className,
  description,
  icon: Icon = Inbox,
  title,
}: EmptyStateProps) {
  return (
    <div
      className={classNames(
        'flex min-h-60 flex-col items-center justify-center rounded-md border border-dashed border-slate-300 bg-white p-8 text-center',
        className,
      )}
    >
      <div className="flex size-12 items-center justify-center rounded-md bg-slate-100 text-brand-navy">
        <Icon size={24} aria-hidden="true" />
      </div>
      <h3 className="mt-4 text-base font-bold text-slate-950">{title}</h3>
      <p className="mt-2 max-w-sm text-sm leading-6 text-slate-600">{description}</p>
      {action ? <div className="mt-5">{action}</div> : null}
    </div>
  )
}
