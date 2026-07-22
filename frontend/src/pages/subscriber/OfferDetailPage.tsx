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
    return <Spinner className="min-h-80" label="Teklif detayı yükleniyor" />
  }

  if (offerQuery.isError) {
    return <ErrorState onRetry={() => offerQuery.refetch()} title="Teklif detayı alınamadı" />
  }

  if (!offerQuery.data) {
    return (
      <EmptyState
        description="Seçilen teklif şu anda görüntülenemiyor."
        title="Teklif bulunamadı"
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
        Tekliflere dön
      </Link>

      <Card className="overflow-hidden border-blue-100 shadow-lg shadow-blue-950/5">
        <CardHeader className="border-blue-100 bg-[#294b98] text-white">
          <div>
            <p className="text-xs font-bold uppercase text-brand-yellow">{offer.campaignNumber}</p>
            <CardTitle className="mt-2 text-xl text-white">{offer.title}</CardTitle>
            <CardDescription className="text-white/72">Kampanya detayları ve avantaj bilgileri</CardDescription>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <Info
              icon={Percent}
              label="İndirim"
              value={`%${offer.discountRate}`}
            />
            <Info
              icon={CalendarDays}
              label="Geçerlilik"
              value={new Date(offer.validUntil).toLocaleDateString('tr-TR')}
            />
            <Info label="Önerim skoru" value={offer.recommendationScore.toString()} />
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
        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Teklifi puanla</CardTitle>
            <CardDescription>
              Deneyimini puanlayarak gelecek tekliflerin daha iyi sekillenmesine yardimci ol.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <StarRating onChange={setStars} value={stars} />
            {stars <= 2 ? (
              <p className="mt-3 text-sm font-semibold text-red-700">
                Düşük puanını dikkate alacağız; sana daha uygun öneriler sunmaya çalışacağız.
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
                Puanlamayı Kaydet
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
    <div className="rounded-md border border-blue-100 bg-blue-50/60 p-4">
      <div className="flex items-center gap-2 text-xs font-bold uppercase text-slate-500">
        {Icon ? <Icon size={16} aria-hidden="true" /> : null}
        {label}
      </div>
      <p className="mt-2 text-2xl font-bold text-brand-navy">{value}</p>
    </div>
  )
}
