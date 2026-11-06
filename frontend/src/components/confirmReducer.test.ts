import { describe, expect, it } from 'vitest'
import { canConfirm, confirmReducer, initialConfirmState } from './confirmReducer'

describe('confirmReducer', () => {
  it('open lưu id đối tượng', () => {
    expect(confirmReducer(initialConfirmState, { type: 'open', targetId: 'q1' })).toEqual({ targetId: 'q1' })
  })

  it('cancel đóng hộp', () => {
    expect(confirmReducer({ targetId: 'q1' }, { type: 'cancel' })).toEqual(initialConfirmState)
  })

  it('sync giữ hộp khi đối tượng vẫn hợp lệ', () => {
    const state = { targetId: 'q1' }
    expect(confirmReducer(state, { type: 'sync', activeId: 'q1' })).toBe(state)
  })

  it('sync đóng hộp khi câu đã kết thúc (activeId null)', () => {
    expect(confirmReducer({ targetId: 'q1' }, { type: 'sync', activeId: null })).toEqual(initialConfirmState)
  })

  it('sync đóng hộp khi đã chuyển sang câu khác', () => {
    expect(confirmReducer({ targetId: 'q1' }, { type: 'sync', activeId: 'q2' })).toEqual(initialConfirmState)
  })

  it('canConfirm: chỉ true khi hộp mở cho đúng đối tượng còn hợp lệ', () => {
    expect(canConfirm({ targetId: 'q1' }, 'q1')).toBe(true)
    expect(canConfirm({ targetId: 'q1' }, 'q2')).toBe(false)
    expect(canConfirm({ targetId: 'q1' }, null)).toBe(false)
    expect(canConfirm(initialConfirmState, 'q1')).toBe(false)
  })
})
