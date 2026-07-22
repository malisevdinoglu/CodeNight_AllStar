import { ArrowLeft, CalendarDays, Percent } from 'lucide-react'
import { useState } from 'react'
import toast from 'react-hot-toast'
import { Link, useParams } from 'react-router-dom'
import { getApiError } from '../../api/errors'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  StarRating,
  Spinner,
} from '../../components/ui'
import { useOfferDetail, useRateOffer, useRespondToOffer } from '../../hooks/useOffers'
import { useAuthStore } from '../../stores/auth.store'

export function OfferDetailPage() {
  const { offerId } = useParams()
  const user = useAuthStore((state) => state.user)
  const offerQuery = useOfferDetail(offerId)
  const respondToOffer = useRespondToOffer(user?.id)
  const rateOffer = useRateOffer(user?.id)
  const [stars, setStars] = useState<1 | 2 | 3 | 4 | 5>(3)

  if (offerQuery.isLoading) {
    return <Spinner className="min-h-80" label="Teklif detayi yukleniyor" />
  }

  if (offerQuery.isError) {
    return <ErrorState onRetry={() => offerQuery.refetch()} title="Teklif detayi alinamadi" />
  }

  if (!offerQuery.data) {
    return (
      <EmptyState
        description="Secilen teklif bulunamadi veya backend henuz bu kaydi dondurmuyor."
        title="Teklif bulunamadi"
      />
    )
  }

  const offer = offerQuery.data

  return (
    <section className="space-y-6">
      <Link
        className="inline-flex items-center gap-2 text-sm font-bold text-brand-navy hover:text-brand-ink"
        to="/offers"
      >
        <ArrowLeft size={17} aria-hidden="true" />
        Tekliflere don
      </Link>

      <Card>
        <CardHeader>
          <CardTitle>{offer.title}</CardTitle>
          <CardDescription>{offer.campaignNumber}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <Info
              icon={Percent}
              label="Indirim"
              value={`%${offer.discountRate}`}
            />
            <Info
              icon={CalendarDays}
              label="Gecerlilik"
              value={new Date(offer.validUntil).toLocaleDateString('tr-TR')}
            />
            <Info label="Onerim skoru" value={offer.recommendationScore.toString()} />
          </div>

          <div className="mt-6 flex flex-wrap items-center gap-2">
            {offer.isPriority ? <Badge tone="brand">Size ozel</Badge> : null}
            <Badge tone="neutral">{offer.type}</Badge>
            <Badge tone={offer.status === 'KABUL' ? 'success' : offer.status === 'RET' ? 'neutral' : 'brand'}>
              {offer.status}
            </Badge>
          </div>

          {offer.status === 'SUNULDU' ? (
            <div className="mt-6 flex flex-wrap gap-3">
              <Button
                isLoading={respondToOffer.isPending}
                onClick={() =>
                  respondToOffer.mutate(
                    { offerId: offer.id, payload: { response: 'KABUL' } },
                    {
                      onSuccess: () => {
                        toast.success('Teklif kabul edildi.')
                        offerQuery.refetch()
                      },
                      onError: (error) => toast.error(getApiError(error).message),
                    },
                  )
                }
              >
                Kabul et
              </Button>
              <Button
                disabled={respondToOffer.isPending}
                onClick={() =>
                  respondToOffer.mutate(
                    { offerId: offer.id, payload: { response: 'RET' } },
                    {
                      onSuccess: () => {
                        toast.success('Teklif kapatildi.')
                        offerQuery.refetch()
                      },
                      onError: (error) => toast.error(getApiError(error).message),
                    },
                  )
                }
                variant="secondary"
              >
                Ilgilenmiyorum
              </Button>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {offer.canRate ? (
        <Card>
          <CardHeader>
            <CardTitle>Teklifi puanla</CardTitle>
            <CardDescription>
              1-2 yildiz seciminde teklif alakasizligi AI geri bildirimine islenir.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <StarRating onChange={setStars} value={stars} />
            {stars <= 2 ? (
              <p className="mt-3 text-sm font-semibold text-red-700">
                Bu teklif neden alakasizdi? Backend geldikten sonra mikro geri bildirim alani
                genisletilecek.
              </p>
            ) : null}
            <div className="mt-5">
              <Button
                isLoading={rateOffer.isPending}
                onClick={() =>
                  rateOffer.mutate(
                    { offerId: offer.id, payload: { stars } },
                    {
                      onSuccess: () => {
                        toast.success('Puanlama kaydedildi.')
                        offerQuery.refetch()
                      },
                      onError: (error) => toast.error(getApiError(error).message),
                    },
                  )
                }
              >
                Puanlamayi kaydet
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : null}
    </section>
  )
}

function Info({
  icon: Icon,
  label,
  value,
}: {
  icon?: typeof Percent
  label: string
  value: string
}) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
      <div className="flex items-center gap-2 text-xs font-bold uppercase text-slate-500">
        {Icon ? <Icon size={16} aria-hidden="true" /> : null}
        {label}
      </div>
      <p className="mt-2 text-2xl font-bold text-slate-950">{value}</p>
    </div>
  )
}
