import { describe, expect, it } from 'vitest'
import { nextRetryDelayMs, RETRY_GIVE_UP_AFTER_MS } from './retryPolicy'

describe('nextRetryDelayMs', () => {
  it('4 lần đầu cách nhau 0, 2, 5, 10 giây', () => {
    expect([0, 1, 2, 3].map((n) => nextRetryDelayMs(n, 0))).toEqual([0, 2_000, 5_000, 10_000])
  })

  it('các lần sau cứ 10 giây một lần', () => {
    expect(nextRetryDelayMs(4, 30_000)).toBe(10_000)
    expect(nextRetryDelayMs(50, 500_000)).toBe(10_000)
  })

  it('còn thử khi chưa đủ 10 phút', () => {
    expect(nextRetryDelayMs(60, RETRY_GIVE_UP_AFTER_MS - 1)).toBe(10_000)
  })

  it('trả null (dừng) khi đã đủ hoặc quá 10 phút', () => {
    expect(nextRetryDelayMs(60, RETRY_GIVE_UP_AFTER_MS)).toBeNull()
    expect(nextRetryDelayMs(0, RETRY_GIVE_UP_AFTER_MS + 1)).toBeNull()
  })
})
