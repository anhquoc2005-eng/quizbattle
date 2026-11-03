import type { IRetryPolicy, RetryContext } from '@microsoft/signalr'

// Mặc định của withAutomaticReconnect (0, 2, 10, 30 giây rồi bỏ) bỏ cuộc sau ~45 giây - quá ngắn khi host
// miễn phí đang ngủ/khởi động lại. Nhịp dưới đây thử nhanh lúc đầu (rớt mạng ngắn), rồi đều đặn để không
// dồn request vào backend đang bật lại.
/** Độ trễ của các lần thử đầu tiên (lần 1-4). */
export const RETRY_DELAYS_MS = [0, 2_000, 5_000, 10_000]
/** Độ trễ của mọi lần thử sau đó. */
export const RETRY_STEADY_DELAY_MS = 10_000
/** Tổng thời gian tự thử lại; quá mốc này thì dừng và để người dùng bấm "Kết nối lại". */
export const RETRY_GIVE_UP_AFTER_MS = 10 * 60 * 1000

/** null = ngừng thử. previousRetryCount là số lần đã thử trước đó (0 cho lần thử đầu). */
export function nextRetryDelayMs(previousRetryCount: number, elapsedMs: number): number | null {
  if (elapsedMs >= RETRY_GIVE_UP_AFTER_MS) {
    return null
  }

  return RETRY_DELAYS_MS[previousRetryCount] ?? RETRY_STEADY_DELAY_MS
}

export const reconnectPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds: (context: RetryContext) =>
    nextRetryDelayMs(context.previousRetryCount, context.elapsedMilliseconds),
}
