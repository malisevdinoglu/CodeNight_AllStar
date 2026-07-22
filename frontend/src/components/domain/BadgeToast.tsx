import { Award } from 'lucide-react'
import { Badge } from '../ui/Badge'

type BadgeToastProps = {
  badgeName: string
  badgeCode: string
}

export function BadgeToast({ badgeCode, badgeName }: BadgeToastProps) {
  return (
    <div className="flex items-start gap-3">
      <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-brand-yellow text-brand-navy">
        <Award size={20} aria-hidden="true" />
      </div>
      <div>
        <p className="text-sm font-bold text-slate-950">Rozet kazanıldı</p>
        <p className="mt-1 text-sm text-slate-600">{badgeName}</p>
        <Badge className="mt-2" tone="brand">
          {badgeCode}
        </Badge>
      </div>
    </div>
  )
}
