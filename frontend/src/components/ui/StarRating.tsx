import { Star } from 'lucide-react'
import { classNames } from '../../lib/classNames'

type StarRatingProps = {
  value: number
  onChange?: (value: 1 | 2 | 3 | 4 | 5) => void
  disabled?: boolean
}

const stars = [1, 2, 3, 4, 5] as const

export function StarRating({ disabled = false, onChange, value }: StarRatingProps) {
  return (
    <div className="inline-flex items-center gap-1" role="radiogroup">
      {stars.map((star) => (
        <button
          aria-checked={value === star}
          className={classNames(
            'flex size-9 items-center justify-center rounded-md transition focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-brand-yellow/40',
            star <= value ? 'text-brand-yellow' : 'text-slate-300',
            disabled ? 'cursor-not-allowed opacity-60' : 'hover:bg-slate-100',
          )}
          disabled={disabled}
          key={star}
          onClick={() => onChange?.(star)}
          role="radio"
          title={`${star} yildiz`}
          type="button"
        >
          <Star
            fill={star <= value ? 'currentColor' : 'none'}
            size={22}
            strokeWidth={2}
            aria-hidden="true"
          />
        </button>
      ))}
    </div>
  )
}
