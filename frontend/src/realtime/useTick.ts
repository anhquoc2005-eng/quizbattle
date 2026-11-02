import { useEffect, useState } from 'react'

/**
 * Ép component re-render định kỳ trong khi active = true. Cần cho các giá trị tính từ đồng hồ (hết giờ
 * chưa, dùng getRemainingMs) ở component cha: nếu không ai trả lời và không có sự kiện server nào tới,
 * React sẽ không tự render lại chỉ vì thời gian trôi qua, nên "hết giờ" sẽ không bao giờ được phát hiện.
 */
export function useTick(active: boolean, intervalMs = 500): void {
  const [, setTick] = useState(0)

  useEffect(() => {
    if (!active) {
      return
    }

    const interval = setInterval(() => setTick((t) => t + 1), intervalMs)
    return () => clearInterval(interval)
  }, [active, intervalMs])
}
