import { useEffect, useState } from 'react'
import { getRemainingSeconds } from '../realtime/serverTime'

/** "{label} Ns" đếm ngược tới mốc server tự chuyển câu - dựa trên offset đồng hồ, không tự đặt mốc. */
export function NextQuestionIn({ label, nextQuestionAt, offsetMs }: { label: string; nextQuestionAt: string; offsetMs: number }) {
  const [seconds, setSeconds] = useState(() => getRemainingSeconds(nextQuestionAt, offsetMs))

  useEffect(() => {
    setSeconds(getRemainingSeconds(nextQuestionAt, offsetMs))
    const update = () => setSeconds(getRemainingSeconds(nextQuestionAt, offsetMs))
    const interval = setInterval(update, 250)
    document.addEventListener('visibilitychange', update)
    return () => {
      clearInterval(interval)
      document.removeEventListener('visibilitychange', update)
    }
  }, [nextQuestionAt, offsetMs])

  return (
    <p className="text-muted">
      {label} <span className="tabular-nums">{seconds}s</span>
    </p>
  )
}
