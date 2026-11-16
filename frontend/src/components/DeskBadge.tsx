import type { DeskStatus } from './deskStatus'

const LABEL = { raised: 'Đã trả lời', correct: 'Trả lời đúng', wrong: 'Trả lời sai hoặc không trả lời' } as const

/**
 * Huy hiệu phủ lên nhân vật (KHÔNG đổi thuộc tính DiceBear - đó là đồ người chơi mua bằng Xu). Hình vẽ bằng SVG đơn giản,
 * không emoji; có aria-label để trạng thái không chỉ dựa vào màu.
 */
export function DeskBadge({ status }: { status: Exclude<DeskStatus, 'idle'> }) {
  return (
    <span className="desk-badge" role="img" aria-label={LABEL[status]}>
      <svg viewBox="0 0 24 24" aria-hidden="true" fill="currentColor">
        {status === 'raised' && (
          <>
            <rect x="5.5" y="6" width="3" height="9" rx="1.5" />
            <rect x="9.2" y="3" width="3" height="12" rx="1.5" />
            <rect x="12.9" y="3.5" width="3" height="11.5" rx="1.5" />
            <rect x="16.6" y="6.5" width="3" height="8.5" rx="1.5" />
            <rect x="5.5" y="11" width="14" height="10" rx="5" />
          </>
        )}
        {status === 'correct' && <path d="M5 12.5l4.5 4.5L19 7.5" fill="none" stroke="currentColor" strokeWidth="3.2" strokeLinecap="round" strokeLinejoin="round" />}
        {status === 'wrong' && <path d="M6.5 6.5l11 11M17.5 6.5l-11 11" fill="none" stroke="currentColor" strokeWidth="3.2" strokeLinecap="round" />}
      </svg>
    </span>
  )
}
