import { describe, expect, it } from 'vitest'
import type { GameError } from '../types/game'
import { getScreen } from './gameScreen'

const joinError = (code: GameError['code']): GameError => ({ method: 'JoinRoom', code, message: 'lỗi' })

describe('getScreen', () => {
  it('đang kết nối và chưa có RoomState thì hiện đang kết nối', () => {
    expect(getScreen('connecting', { status: null }, null)).toBe('connecting')
  })

  it('đã kết nối nhưng chưa có RoomState và chưa có lỗi thì vẫn đang kết nối', () => {
    expect(getScreen('connected', { status: null }, null)).toBe('connecting')
  })

  it.each(['unknown', 'conflict', 'unauthorized', 'not_found'] as const)('JoinRoom lỗi %s khi chưa có RoomState thì hiện lỗi', (code) => {
    expect(getScreen('connected', { status: null }, joinError(code))).toBe('joinError')
  })

  it('lỗi của method khác khi chưa có RoomState thì vẫn đang kết nối', () => {
    expect(getScreen('connected', { status: null }, { method: 'SubmitAnswer', code: 'unknown', message: 'x' })).toBe('connecting')
  })

  it('JoinRoom lỗi nhưng đã có RoomState (lỗi lúc vào lại) thì vẫn hiện trận', () => {
    expect(getScreen('connected', { status: 'InProgress' }, joinError('unknown'))).toBe('game')
  })

  it('mất kết nối hẳn khi chưa có RoomState thì hiện nút kết nối lại, không kẹt đang kết nối', () => {
    expect(getScreen('disconnected', { status: null }, null)).toBe('disconnected')
  })

  it('đang kết nối lại khi đã có RoomState thì vẫn hiện trận', () => {
    expect(getScreen('reconnecting', { status: 'Lobby' }, null)).toBe('game')
  })
})
