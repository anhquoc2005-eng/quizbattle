import { useEffect, useId, useRef } from 'react'

/**
 * Hộp "Phiên đăng nhập đã hết hạn" ở trang vào phòng: buộc chọn đăng nhập lại hoặc vào làm khách, không có nút đóng
 * (FR-021: không được âm thầm thành khách). Tự dựng bằng div role="dialog" thay vì <dialog>.showModal() để kiểm thử
 * được trong jsdom; tiêu điểm chuyển vào hộp khi mở.
 */
export function SessionExpiredDialog({ onLoginAgain, onContinueAsGuest }: { onLoginAgain: () => void; onContinueAsGuest: () => void }) {
  const titleId = useId()
  const loginButton = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    loginButton.current?.focus()
  }, [])

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-ground/80 p-4">
      <div role="dialog" aria-modal="true" aria-labelledby={titleId} className="grid w-[min(92vw,24rem)] gap-4 rounded-[20px] bg-surface p-6 text-ink">
        <h2 id={titleId}>Phiên đăng nhập đã hết hạn</h2>
        <p className="text-muted">Đăng nhập lại để XP và Xu của trận này được tính, hoặc vào làm khách (khách không nhận thưởng).</p>
        <div className="flex flex-wrap justify-end gap-2">
          <button className="btn btn-ghost" type="button" onClick={onContinueAsGuest}>
            Vào làm khách
          </button>
          <button ref={loginButton} className="btn btn-primary" type="button" onClick={onLoginAgain}>
            Đăng nhập lại
          </button>
        </div>
      </div>
    </div>
  )
}
