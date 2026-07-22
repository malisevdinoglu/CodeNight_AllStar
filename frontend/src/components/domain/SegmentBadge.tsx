import type { Segment } from '../../api/types'
import { Badge } from '../ui/Badge'

const segmentLabels: Record<Segment, string> = {
  YUKSEK_DEGER: 'Yüksek değer',
  RISKLI_KAYIP: 'Riskli kayıp',
  YENI_ABONE: 'Yeni abone',
  PASIF: 'Pasif',
  BELIRSIZ: 'Belirsiz',
}

const segmentTones: Record<Segment, Parameters<typeof Badge>[0]['tone']> = {
  YUKSEK_DEGER: 'success',
  RISKLI_KAYIP: 'danger',
  YENI_ABONE: 'info',
  PASIF: 'neutral',
  BELIRSIZ: 'warning',
}

export function SegmentBadge({ segment }: { segment: Segment }) {
  return <Badge tone={segmentTones[segment]}>{segmentLabels[segment]}</Badge>
}
