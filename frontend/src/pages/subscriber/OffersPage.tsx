import toast from 'react-hot-toast'
import { OfferCard } from '../../components/domain'
import {
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
      <div className="rounded-md bg-[#294b98] p-5 text-white shadow-lg shadow-blue-950/10 sm:p-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-sm font-bold uppercase text-brand-yellow">Müşteri kampanyaları</p>
            <h1 className="mt-2 text-2xl font-bold">Kişisel Teklifler</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-white/75">
              Sana en uygun kampanya ve avantajları tek ekranda inceleyebilirsin.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-3 lg:min-w-[30rem]">
            <Summary label="Aktif teklif" value={offers.filter((item) => item.status === 'SUNULDU').length} />
            <Summary label="Kabul edilen" value={offers.filter((item) => item.status === 'KABUL').length} />
            <Summary label="Puanlanabilir" value={offers.filter((item) => item.canRate).length} />
          </div>
        </div>
      </div>

      {offersQuery.isLoading ? <Spinner className="min-h-60" label="Teklifler yükleniyor" /> : null}
      {offersQuery.isError ? (
        <ErrorState onRetry={() => offersQuery.refetch()} title="Teklifler alınamadı" />
      ) : null}
      {!offersQuery.isLoading && !offersQuery.isError && sortedOffers.length === 0 ? (
        <EmptyState
          description="Şu anda sana özel aktif teklif bulunmuyor."
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
    <div className="rounded-md border border-white/15 bg-white/10 p-4">
      <p className="text-xs font-bold uppercase text-white/70">{label}</p>
      <p className="mt-2 text-2xl font-bold text-brand-yellow">{value}</p>
    </div>
  )
}
