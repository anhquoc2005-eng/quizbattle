import { describe, expect, it } from 'vitest'
import { computeClockOffsetMs, getRemainingMs, getRemainingSeconds } from './serverTime'

describe('computeClockOffsetMs', () => {
  it('= 0 khi đồng hồ client và server khớp nhau', () => {
    const now = 1_000_000
    expect(computeClockOffsetMs(new Date(now).toISOString(), now)).toBe(0)
  })

  it('dương khi đồng hồ client chạy chậm hơn server (server đi trước)', () => {
    const now = 1_000_000
    expect(computeClockOffsetMs(new Date(now + 5000).toISOString(), now)).toBe(5000)
  })

  it('âm khi đồng hồ client chạy nhanh hơn server (server đi sau)', () => {
    const now = 1_000_000
    expect(computeClockOffsetMs(new Date(now - 5000).toISOString(), now)).toBe(-5000)
  })
})

describe('getRemainingMs', () => {
  it('ra đúng thời gian còn lại khi đồng hồ không lệch', () => {
    const now = 1_000_000
    const deadline = new Date(now + 10_000).toISOString()
    expect(getRemainingMs(deadline, 0, now)).toBe(10_000)
  })

  it('đồng hồ client lệch +5 giây (offset âm, client nhanh hơn) vẫn ra đúng thời gian còn lại', () => {
    const now = 1_000_000
    const deadline = new Date(now + 10_000).toISOString()
    expect(getRemainingMs(deadline, -5000, now)).toBe(15_000)
  })

  it('đồng hồ client lệch -5 giây (offset dương, client chậm hơn) vẫn ra đúng thời gian còn lại', () => {
    const now = 1_000_000
    const deadline = new Date(now + 10_000).toISOString()
    expect(getRemainingMs(deadline, 5000, now)).toBe(5_000)
  })

  it('không bao giờ âm khi đã quá hạn', () => {
    const now = 1_000_000
    const deadline = new Date(now - 5000).toISOString()
    expect(getRemainingMs(deadline, 0, now)).toBe(0)
  })
})

describe('getRemainingSeconds', () => {
  const now = 1_000_000

  it('đếm ngược "câu tiếp theo sau Ns": còn 6 giây -> 6', () => {
    expect(getRemainingSeconds(new Date(now + 6000).toISOString(), 0, now)).toBe(6)
  })

  it('làm tròn lên: còn 5,2 giây -> 6', () => {
    expect(getRemainingSeconds(new Date(now + 5200).toISOString(), 0, now)).toBe(6)
  })

  it('bù lệch đồng hồ và không âm khi quá mốc', () => {
    expect(getRemainingSeconds(new Date(now + 6000).toISOString(), 2000, now)).toBe(4)
    expect(getRemainingSeconds(new Date(now - 1000).toISOString(), 0, now)).toBe(0)
  })
})
