import { describe, expect, it } from 'vitest'
import type { GameError } from '../types/game'
import { shouldDiscardSession } from './sessionErrors'

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
