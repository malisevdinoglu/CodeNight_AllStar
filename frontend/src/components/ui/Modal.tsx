import { X } from 'lucide-react'
import type { ReactNode } from 'react'
import { Button } from './Button'

type ModalProps = {
  isOpen: boolean
  title: string
  description?: string
  children: ReactNode
  footer?: ReactNode
  onClose: () => void
}

export function Modal({ children, description, footer, isOpen, onClose, title }: ModalProps) {
  if (!isOpen) {
    return null
  }

  return (
    <div
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/55 px-4 py-6"
      role="dialog"
    >
      <div className="w-full max-w-lg rounded-md border border-slate-200 bg-white shadow-panel">
        <div className="flex items-start justify-between gap-4 border-b border-slate-200 p-5">
          <div>
            <h2 className="text-lg font-bold text-slate-950">{title}</h2>
            {description ? (
              <p className="mt-1 text-sm leading-6 text-slate-600">{description}</p>
            ) : null}
          </div>
          <Button leftIcon={<X size={18} aria-hidden="true" />} onClick={onClose} size="icon" variant="ghost">
            Kapat
          </Button>
        </div>
        <div className="p-5">{children}</div>
        {footer ? (
          <div className="flex justify-end gap-3 border-t border-slate-200 p-5">{footer}</div>
        ) : null}
      </div>
    </div>
  )
}
