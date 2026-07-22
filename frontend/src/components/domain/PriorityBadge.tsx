import type { Priority } from '../../api/types'
import { Badge } from '../ui/Badge'

const priorityLabels: Record<Priority, string> = {
  DUSUK: 'Düşük',
  ORTA: 'Orta',
  YUKSEK: 'Yüksek',
  KRITIK: 'Kritik',
}

const priorityTones: Record<Priority, Parameters<typeof Badge>[0]['tone']> = {
  DUSUK: 'neutral',
  ORTA: 'info',
  YUKSEK: 'warning',
  KRITIK: 'danger',
}

export function PriorityBadge({ priority }: { priority: Priority }) {
  return <Badge tone={priorityTones[priority]}>{priorityLabels[priority]}</Badge>
}
