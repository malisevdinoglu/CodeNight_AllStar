import type { HTMLAttributes, ReactNode } from 'react'
import { classNames } from '../../lib/classNames'

type CardProps = HTMLAttributes<HTMLDivElement>

export function Card({ children, className, ...props }: CardProps) {
  return (
    <div
      className={classNames('rounded-md border border-slate-200 bg-white shadow-sm', className)}
      {...props}
    >
      {children}
    </div>
  )
}

export function CardHeader({
  action,
  children,
  className,
  ...props
}: CardProps & { action?: ReactNode }) {
  return (
    <div
      className={classNames(
        'flex flex-col gap-3 border-b border-slate-200 p-5 sm:flex-row sm:items-center sm:justify-between',
        className,
      )}
      {...props}
    >
      <div>{children}</div>
      {action ? <div className="shrink-0">{action}</div> : null}
    </div>
  )
}

export function CardTitle({ children, className, ...props }: HTMLAttributes<HTMLHeadingElement>) {
  return (
    <h2 className={classNames('text-base font-bold text-slate-950', className)} {...props}>
      {children}
    </h2>
  )
}

export function CardDescription({
  children,
  className,
  ...props
}: HTMLAttributes<HTMLParagraphElement>) {
  return (
    <p className={classNames('mt-1 text-sm leading-6 text-slate-600', className)} {...props}>
      {children}
    </p>
  )
}

export function CardContent({ children, className, ...props }: CardProps) {
  return (
    <div className={classNames('p-5', className)} {...props}>
      {children}
    </div>
  )
}
