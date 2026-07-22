import { AlertTriangle } from 'lucide-react'
import { Button } from './Button'

type ErrorStateProps = {
  title?: string
  description?: string
  retryLabel?: string
  onRetry?: () => void
}

export function ErrorState({
  description = 'Veri alınırken bir sorun oluştu.',
  onRetry,
  retryLabel = 'Tekrar dene',
  title = 'Bir şey ters gitti',
}: ErrorStateProps) {
  return (
    <div className="flex min-h-60 flex-col items-center justify-center rounded-md border border-red-200 bg-red-50 p-8 text-center">
      <div className="flex size-12 items-center justify-center rounded-md bg-white text-red-600">
        <AlertTriangle size={24} aria-hidden="true" />
      </div>
      <h3 className="mt-4 text-base font-bold text-red-950">{title}</h3>
      <p className="mt-2 max-w-sm text-sm leading-6 text-red-700">{description}</p>
      {onRetry ? (
        <Button className="mt-5" onClick={onRetry} variant="danger">
          {retryLabel}
        </Button>
      ) : null}
    </div>
  )
}
