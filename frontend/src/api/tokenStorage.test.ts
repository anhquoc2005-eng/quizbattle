// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AuthResponse } from '../types/auth'
import {
  clearAuth,
  clearSessionExpiredFlag,
  isSessionExpiredFlagSet,
  isTokenExpired,
  loadAuth,
  markSessionExpired,
  saveAuth,
  subscribeSessionExpired,
} from './tokenStorage'

function auth(expiresAt: string): AuthResponse {
  return { token: 't', expiresAt, user: { id: 'u1', email: 'a@b.c', displayName: 'A' } }
}

beforeEach(() => {
  localStorage.clear()
  clearSessionExpiredFlag()
})

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

describe('cờ phiên đăng nhập hết hạn', () => {
  it('loadAuth_ExpiredToken_ClearsTokenAndSetsExpiredFlag', () => {
    saveAuth(auth(new Date(Date.now() - 1000).toISOString()))

    expect(loadAuth()).toBeNull()

    expect(localStorage.getItem('quizbattle.auth')).toBeNull()
    expect(isSessionExpiredFlagSet()).toBe(true)
  })

  it('loadAuth_ValidToken_DoesNotSetFlag', () => {
    saveAuth(auth(new Date(Date.now() + 60_000).toISOString()))

    expect(loadAuth()).not.toBeNull()

    expect(isSessionExpiredFlagSet()).toBe(false)
  })

  it('loadAuth_NoStoredAuth_DoesNotSetFlag', () => {
    expect(loadAuth()).toBeNull()

    expect(isSessionExpiredFlagSet()).toBe(false)
  })

  it('clearSessionExpiredFlag_RemovesFlag', () => {
    markSessionExpired()

    clearSessionExpiredFlag()

    expect(isSessionExpiredFlagSet()).toBe(false)
  })

  it('logout_DoesNotSetFlag', () => {
    saveAuth(auth(new Date(Date.now() + 60_000).toISOString()))

    clearAuth() // đăng xuất chủ động chỉ xóa token

    expect(isSessionExpiredFlagSet()).toBe(false)
  })

  it('cờ nằm trong localStorage để tab khác cũng thấy', () => {
    markSessionExpired()

    expect(localStorage.getItem('quizbattle.sessionExpired')).not.toBeNull()
  })

  it('subscribe: được báo khi đặt và khi xóa cờ, và ngừng báo sau khi hủy đăng ký', () => {
    const listener = vi.fn()
    const unsubscribe = subscribeSessionExpired(listener)

    markSessionExpired()
    clearSessionExpiredFlag()
    expect(listener).toHaveBeenCalledTimes(2)

    unsubscribe()
    markSessionExpired()
    expect(listener).toHaveBeenCalledTimes(2)
  })

  it('subscribe: sự kiện storage của tab khác về khóa cờ cũng báo cho listener', () => {
    const listener = vi.fn()
    const unsubscribe = subscribeSessionExpired(listener)

    window.dispatchEvent(new StorageEvent('storage', { key: 'quizbattle.sessionExpired' }))

    expect(listener).toHaveBeenCalledTimes(1)
    unsubscribe()
  })
})
