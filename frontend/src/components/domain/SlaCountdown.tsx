import { Clock3 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { classNames } from '../../lib/classNames'

type SlaCountdownProps = {
  remainingSeconds: number
  breached?: boolean
}

function formatDuration(totalSeconds: number) {
  const absSeconds = Math.abs(totalSeconds)
  const hours = Math.floor(absSeconds / 3600)
  const minutes = Math.floor((absSeconds % 3600) / 60)
  const seconds = absSeconds % 60

  if (hours > 0) {
    return `${hours}s ${minutes}dk`
  }

  return `${minutes}dk ${seconds.toString().padStart(2, '0')}sn`
}

export function SlaCountdown({ breached = false, remainingSeconds }: SlaCountdownProps) {
  const [secondsLeft, setSecondsLeft] = useState(remainingSeconds)
  const isBreached = breached || secondsLeft < 0

  useEffect(() => {
    setSecondsLeft(remainingSeconds)
  }, [remainingSeconds])

  useEffect(() => {
    const timer = window.setInterval(() => {
      setSecondsLeft((current) => current - 1)
    }, 1000)

    return () => window.clearInterval(timer)
  }, [])

  return (
    <span
      className={classNames(
        'inline-flex min-h-8 items-center gap-2 rounded-md px-3 text-sm font-bold',
        isBreached ? 'bg-red-50 text-red-700' : 'bg-slate-100 text-slate-700',
      )}
    >
      <Clock3 size={16} aria-hidden="true" />
      {isBreached ? 'SLA ASILDI' : formatDuration(secondsLeft)}
    </span>
  )
}
