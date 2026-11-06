import { useEffect, useState } from 'react'
import { getRemainingMs } from '../realtime/serverTime'

/** Thanh đếm ngược (accent, vì là dữ liệu) - dùng offset đồng hồ đã bù, không tự đặt mốc thời gian. */
export function Countdown({ deadline, offsetMs, totalSeconds }: { deadline: string; offsetMs: number; totalSeconds: number }) {
  const [remainingMs, setRemainingMs] = useState(() => getRemainingMs(deadline, offsetMs))

  useEffect(() => {
    setRemainingMs(getRemainingMs(deadline, offsetMs))
    const update = () => setRemainingMs(getRemainingMs(deadline, offsetMs))
    const interval = setInterval(update, 250)
    // Tab nền bị trình duyệt giãn nhịp setInterval: tính lại ngay khi tab hiện lại.
    document.addEventListener('visibilitychange', update)
    return () => {
      clearInterval(interval)
      document.removeEventListener('visibilitychange', update)
    }
  }, [deadline, offsetMs])

  const remainingSeconds = Math.ceil(remainingMs / 1000)
  const percent = totalSeconds > 0 ? Math.min(100, Math.max(0, (remainingMs / 1000 / totalSeconds) * 100)) : 0

  return (
    <div className="grid gap-1.5">
      <div className="flex items-center justify-between text-xs text-faint">
        <span>Thời gian còn lại</span>
        <span className="tabular-nums text-accent">{remainingSeconds}s</span>
      </div>
      <div
        className="h-1.5 overflow-hidden rounded-full bg-raised"
        role="progressbar"
        aria-valuenow={remainingSeconds}
        aria-valuemin={0}
        aria-valuemax={totalSeconds}
        aria-label="Thời gian còn lại"
      >
        <span className="block h-full bg-accent" style={{ width: `${percent}%` }} />
      </div>
    </div>
  )
}
