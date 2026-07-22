import { Loader2 } from 'lucide-react'
import { classNames } from '../../lib/classNames'

type SpinnerProps = {
  label?: string
  className?: string
}

export function Spinner({ className, label = 'Yükleniyor' }: SpinnerProps) {
  return (
    <div className={classNames('flex items-center justify-center gap-2 text-sm text-slate-600', className)}>
      <Loader2 className="animate-spin text-brand-navy" size={18} aria-hidden="true" />
      <span>{label}</span>
    </div>
  )
}
