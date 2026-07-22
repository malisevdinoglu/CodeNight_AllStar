import toast from 'react-hot-toast'
import { OfferCard } from '../../components/domain'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  Spinner,
} from '../../components/ui'
import { useRespondToOffer, useSubscriberOffers } from '../../hooks/useOffers'
import { useAuthStore } from '../../stores/auth.store'
import { getApiError } from '../../api/errors'

export function OffersPage() {
  const user = useAuthStore((state) => state.user)
  const offersQuery = useSubscriberOffers(user?.id)
  const respondToOffer = useRespondToOffer(user?.id)
  const offers = offersQuery.data ?? []
  const sortedOffers = [...offers].sort((first, second) => {
    if (first.isPriority !== second.isPriority) {
      return first.isPriority ? -1 : 1
    }

    return second.recommendationScore - first.recommendationScore
  })

  return (
    <section className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Kisisel teklifler</CardTitle>
          <CardDescription>
            Oncelikli teklifler ustte gorunur; kabul/ret aksiyonlari optimistic olarak yenilenir.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-3">
            <Summary label="Aktif teklif" value={offers.filter((item) => item.status === 'SUNULDU').length} />
            <Summary label="Kabul edilen" value={offers.filter((item) => item.status === 'KABUL').length} />
            <Summary label="Puanlanabilir" value={offers.filter((item) => item.canRate).length} />
          </div>
        </CardContent>
      </Card>

      {offersQuery.isLoading ? <Spinner className="min-h-60" label="Teklifler yukleniyor" /> : null}
      {offersQuery.isError ? (
        <ErrorState onRetry={() => offersQuery.refetch()} title="Teklifler alinamadi" />
      ) : null}
      {!offersQuery.isLoading && !offersQuery.isError && sortedOffers.length === 0 ? (
        <EmptyState
          description="Su anda sana ozel aktif teklif bulunmuyor."
          title="Teklif yok"
        />
      ) : null}

      <div className="grid gap-4">
        {sortedOffers.map((offer) => (
          <OfferCard
            isBusy={respondToOffer.isPending}
            key={offer.id}
            offer={offer}
            onRespond={(response) =>
              respondToOffer.mutate(
                { offerId: offer.id, payload: { response } },
                {
                  onSuccess: () =>
                    toast.success(response === 'KABUL' ? 'Teklif kabul edildi.' : 'Teklif kapatildi.'),
                  onError: (error) => toast.error(getApiError(error).message),
                },
              )
            }
          />
        ))}
      </div>
    </section>
  )
}

function Summary({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
      <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-bold text-brand-navy">{value}</p>
    </div>
  )
}
