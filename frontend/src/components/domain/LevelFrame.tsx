import type { ReactNode } from 'react'
import type { Level } from '../../api/types'
import { classNames } from '../../lib/classNames'

type LevelFrameProps = {
  level: Level
  children: ReactNode
}

const levelClasses: Record<Level, string> = {
  BRONZ: 'border-amber-700 bg-amber-50',
  GUMUS: 'border-slate-400 bg-slate-50',
  ALTIN: 'border-brand-yellow bg-brand-yellow/10',
  PLATIN: 'border-cyan-400 bg-cyan-50',
}

export function LevelFrame({ children, level }: LevelFrameProps) {
  return (
    <div className={classNames('rounded-md border-2 p-4 shadow-sm', levelClasses[level])}>
      {children}
    </div>
  )
}
