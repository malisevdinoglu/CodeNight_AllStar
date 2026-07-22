import { Gift, Smartphone, Sparkles, TrendingUp } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { CampaignType, OfferDto } from '../../api/types'
import { Button } from '../ui/Button'
import { Badge } from '../ui/Badge'

type OfferCardProps = {
  offer: OfferDto
  isBusy?: boolean
  onRespond: (response: 'KABUL' | 'RET') => void
}

const typeIcons: Record<CampaignType, typeof Gift> = {
  EK_PAKET: Gift,
  TARIFE_YUKSELTME: TrendingUp,
  CIHAZ_FIRSATI: Smartphone,
  SADAKAT: Sparkles,
}

const statusTone: Record<OfferDto['status'], Parameters<typeof Badge>[0]['tone']> = {
  SUNULDU: 'brand',
  KABUL: 'success',
  RET: 'neutral',
}

export function OfferCard({ isBusy = false, offer, onRespond }: OfferCardProps) {
  const Icon = typeIcons[offer.type]

  return (
    <article className="rounded-md border border-blue-100 bg-white p-5 shadow-sm shadow-blue-950/5 transition hover:-translate-y-0.5 hover:border-brand-navy/30 hover:shadow-lg hover:shadow-blue-950/10">
      <div className="flex items-start gap-4">
        <div className="flex size-12 shrink-0 items-center justify-center rounded-md bg-[#294b98] text-brand-yellow">
          <Icon size={24} aria-hidden="true" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            {offer.isPriority ? <Badge tone="brand">Size özel</Badge> : null}
            <Badge tone={statusTone[offer.status]}>{offer.status}</Badge>
            <Badge tone="neutral">{offer.type}</Badge>
          </div>
          <h2 className="mt-3 text-lg font-bold text-slate-950">{offer.title}</h2>
          <p className="mt-2 text-sm leading-6 text-slate-600">
            {offer.campaignNumber} - %{offer.discountRate} avantaj - uygunluk puanı{' '}
            {offer.recommendationScore}
          </p>
          <div className="mt-4 h-2 rounded-full bg-blue-50">
            <div
              className="h-2 rounded-full bg-brand-yellow"
              style={{ width: `${offer.recommendationScore}%` }}
            />
          </div>
        </div>
      </div>

      <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-xs font-semibold text-slate-500">
          Geçerlilik: {new Date(offer.validUntil).toLocaleDateString('tr-TR')}
        </p>
        <div className="flex flex-wrap gap-2">
          <Link
            className="inline-flex h-10 items-center justify-center rounded-md border border-blue-100 bg-white px-4 text-sm font-bold text-brand-navy transition hover:border-brand-navy/40 hover:bg-blue-50"
            to={`/offers/${offer.id}`}
          >
            Detay
          </Link>
          {offer.status === 'SUNULDU' ? (
            <>
              <Button
                className="bg-brand-yellow text-brand-navy hover:bg-yellow-300 focus-visible:ring-brand-yellow/30"
                isLoading={isBusy}
                onClick={() => onRespond('KABUL')}
                size="sm"
              >
                Kabul et
              </Button>
              <Button
                disabled={isBusy}
                onClick={() => onRespond('RET')}
                size="sm"
                variant="secondary"
              >
                İlgilenmiyorum
              </Button>
            </>
          ) : null}
        </div>
      </div>
    </article>
  )
}
