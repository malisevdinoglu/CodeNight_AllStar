import { Loader2 } from 'lucide-react'
import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { classNames } from '../../lib/classNames'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'
type ButtonSize = 'sm' | 'md' | 'icon'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant
  size?: ButtonSize
  isLoading?: boolean
  leftIcon?: ReactNode
  rightIcon?: ReactNode
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-brand-navy text-white hover:bg-brand-ink focus-visible:ring-brand-navy/25',
  secondary:
    'border border-slate-200 bg-white text-slate-800 hover:border-brand-navy/40 hover:bg-slate-50 focus-visible:ring-brand-navy/15',
  ghost: 'text-slate-700 hover:bg-slate-100 focus-visible:ring-slate-400/25',
  danger: 'bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-600/25',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'h-9 px-3 text-sm',
  md: 'h-11 px-4 text-sm',
  icon: 'size-10 p-0',
}

export function Button({
  children,
  className,
  disabled,
  isLoading = false,
  leftIcon,
  rightIcon,
  size = 'md',
  type = 'button',
  variant = 'primary',
  ...props
}: ButtonProps) {
  return (
    <button
      className={classNames(
        'inline-flex items-center justify-center gap-2 rounded-md font-bold transition focus-visible:outline-none focus-visible:ring-4 disabled:cursor-not-allowed disabled:opacity-60',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      disabled={disabled || isLoading}
      type={type}
      {...props}
    >
      {isLoading ? <Loader2 className="animate-spin" size={18} aria-hidden="true" /> : leftIcon}
      {size === 'icon' ? <span className="sr-only">{children}</span> : children}
      {!isLoading ? rightIcon : null}
    </button>
  )
}
