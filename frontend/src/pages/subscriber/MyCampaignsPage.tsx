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
    return <Spinner className="min-h-80" label="Kampanyalarim yukleniyor" />
  }

  if (offersQuery.isError) {
    return <ErrorState onRetry={() => offersQuery.refetch()} title="Kampanyalar alinamadi" />
  }

  if (respondedOffers.length === 0) {
    return (
      <EmptyState
        description="Kabul ettigin veya kapattigin kampanya henuz bulunmuyor."
        title="Kampanya gecmisi yok"
      />
    )
  }

  return (
    <section className="space-y-4">
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
