import { OfferCard } from '../../components/domain'
import { EmptyState, ErrorState, Spinner } from '../../components/ui'
import { useRespondToOffer, useSubscriberOffers } from '../../hooks/useOffers'
import { useAuthStore } from '../../stores/auth.store'

export function MyCampaignsPage() {
  const user = useAuthStore((state) => state.user)
  const offersQuery = useSubscriberOffers(user?.id)
  const respondToOffer = useRespondToOffer(user?.id)
  const respondedOffers = (offersQuery.data ?? []).filter((item) => item.status !== 'SUNULDU')

  if (offersQuery.isLoading) {
    return <Spinner className="min-h-80" label="Kampanyalarım yükleniyor" />
  }

  if (offersQuery.isError) {
    return <ErrorState onRetry={() => offersQuery.refetch()} title="Kampanyalar alınamadı" />
  }

  if (respondedOffers.length === 0) {
    return (
      <EmptyState
        description="Kabul ettiğin veya kapattığın kampanya henüz bulunmuyor."
        title="Kampanya geçmişi yok"
      />
    )
  }

  return (
    <section className="space-y-4">
      <div className="rounded-md bg-[#294b98] p-5 text-white shadow-lg shadow-blue-950/10 sm:p-6">
        <p className="text-sm font-bold uppercase text-brand-yellow">Kampanya geçmişi</p>
        <h1 className="mt-2 text-2xl font-bold">Kampanyalarım</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-white/75">
          Kabul ettiğin ve kapattığın teklifleri buradan takip edebilirsin.
        </p>
      </div>

      {respondedOffers.map((offer) => (
        <OfferCard
          isBusy={respondToOffer.isPending}
          key={offer.id}
          offer={offer}
          onRespond={(response) =>
            respondToOffer.mutate({ offerId: offer.id, payload: { response } })
          }
        />
      ))}
    </section>
  )
}
