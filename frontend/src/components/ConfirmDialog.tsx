import { useEffect, useRef } from 'react'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: string
  confirmLabel: string
  pending?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/** Hộp xác nhận dùng thẻ <dialog> native (có sẵn focus trap và Esc). Trạng thái mở/đóng do cha điều khiển. */
export function ConfirmDialog({ open, title, message, confirmLabel, pending, onConfirm, onCancel }: ConfirmDialogProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      className="m-auto w-[min(92vw,24rem)] rounded-[20px] bg-surface p-6 text-ink backdrop:bg-ground/80"
      onCancel={(event) => {
        event.preventDefault()
        onCancel()
      }}
    >
      <div className="grid gap-4">
        <h2>{title}</h2>
        <p className="text-muted">{message}</p>
        <div className="flex justify-end gap-2">
          <button className="btn btn-ghost" type="button" onClick={onCancel}>
            Hủy
          </button>
          <button className="btn btn-primary" type="button" onClick={onConfirm} disabled={pending}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  )
}
