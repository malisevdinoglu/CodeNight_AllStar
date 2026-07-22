import type { HTMLAttributes } from 'react'
import { classNames } from '../../lib/classNames'

type BadgeTone = 'neutral' | 'brand' | 'success' | 'warning' | 'danger' | 'info'

type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  tone?: BadgeTone
}

const toneClasses: Record<BadgeTone, string> = {
  neutral: 'bg-slate-100 text-slate-700 ring-slate-200',
  brand: 'bg-brand-yellow/20 text-brand-navy ring-brand-yellow/60',
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  warning: 'bg-amber-50 text-amber-700 ring-amber-200',
  danger: 'bg-red-50 text-red-700 ring-red-200',
  info: 'bg-sky-50 text-sky-700 ring-sky-200',
}

export function Badge({ children, className, tone = 'neutral', ...props }: BadgeProps) {
  return (
    <span
      className={classNames(
        'inline-flex min-h-6 items-center rounded-sm px-2 py-1 text-xs font-bold ring-1',
        toneClasses[tone],
        className,
      )}
      {...props}
    >
      {children}
    </span>
  )
}
