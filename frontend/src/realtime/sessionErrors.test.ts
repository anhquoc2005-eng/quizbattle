import { describe, expect, it } from 'vitest'
import type { GameError } from '../types/game'
import { isSessionReplaced, shouldDiscardSession, shouldIgnoreError } from './sessionErrors'

function error(overrides: Partial<GameError> = {}): GameError {
  return { method: 'JoinRoom', code: 'unauthorized', message: 'Phiên không hợp lệ.', ...overrides }
}

describe('shouldDiscardSession', () => {
  it('JoinRoom + unauthorized -> xóa phiên', () => {
    expect(shouldDiscardSession(error({ method: 'JoinRoom', code: 'unauthorized' }))).toBe(true)
  })

  it('JoinRoom + not_found -> xóa phiên', () => {
    expect(shouldDiscardSession(error({ method: 'JoinRoom', code: 'not_found' }))).toBe(true)
  })

  it('JoinRoom + conflict (lỗi tạm thời) -> KHÔNG xóa phiên', () => {
    expect(shouldDiscardSession(error({ method: 'JoinRoom', code: 'conflict' }))).toBe(false)
  })

  it('JoinRoom + unknown (lỗi tạm thời) -> KHÔNG xóa phiên', () => {
    expect(shouldDiscardSession(error({ method: 'JoinRoom', code: 'unknown' }))).toBe(false)
  })

  it('SubmitAnswer + unauthorized (không phải JoinRoom) -> KHÔNG xóa phiên', () => {
    expect(shouldDiscardSession(error({ method: 'SubmitAnswer', code: 'unauthorized' }))).toBe(false)
  })
})

describe('session_replaced (cùng tài khoản vào lại từ thiết bị khác)', () => {
  it('JoinRoom + session_replaced -> xóa phiên đã lưu', () => {
    expect(shouldDiscardSession(error({ method: 'JoinRoom', code: 'session_replaced' }))).toBe(true)
  })

  it('SubmitAnswer + session_replaced -> xóa phiên đã lưu (khác các lỗi SubmitAnswer khác)', () => {
    expect(shouldDiscardSession(error({ method: 'SubmitAnswer', code: 'session_replaced' }))).toBe(true)
  })

  it('isSessionReplaced chỉ đúng với mã session_replaced', () => {
    expect(isSessionReplaced(error({ code: 'session_replaced' }))).toBe(true)
    expect(isSessionReplaced(error({ code: 'unauthorized' }))).toBe(false)
  })

  it('session_replaced không bị bỏ qua như conflict của EndQuestion/NextQuestion', () => {
    expect(shouldIgnoreError(error({ method: 'NextQuestion', code: 'session_replaced' }))).toBe(false)
  })
})

describe('shouldIgnoreError', () => {
  it('EndQuestion + conflict -> bỏ qua', () => {
    expect(shouldIgnoreError(error({ method: 'EndQuestion', code: 'conflict' }))).toBe(true)
  })

  it('NextQuestion + conflict -> bỏ qua', () => {
    expect(shouldIgnoreError(error({ method: 'NextQuestion', code: 'conflict' }))).toBe(true)
  })

  it('EndQuestion + unauthorized -> vẫn hiện', () => {
    expect(shouldIgnoreError(error({ method: 'EndQuestion', code: 'unauthorized' }))).toBe(false)
  })

  it('StartGame + conflict -> vẫn hiện', () => {
    expect(shouldIgnoreError(error({ method: 'StartGame', code: 'conflict' }))).toBe(false)
  })
})
