import { describe, expect, it } from 'vitest'
import { isTokenExpired } from './tokenStorage'

describe('isTokenExpired', () => {
  it('trả về true khi thời điểm đã qua', () => {
    expect(isTokenExpired(new Date(Date.now() - 1000).toISOString())).toBe(true)
  })

  it('trả về false khi thời điểm còn ở tương lai', () => {
    expect(isTokenExpired(new Date(Date.now() + 60_000).toISOString())).toBe(false)
  })

  it('trả về true khi không đọc được ngày', () => {
    expect(isTokenExpired('không phải ngày')).toBe(true)
  })
})
