import { describe, expect, it } from 'vitest'
import { canConfirm, confirmReducer, initialConfirmState } from './confirmReducer'

describe('confirmReducer', () => {
  it('open lưu id đối tượng', () => {
    expect(confirmReducer(initialConfirmState, { type: 'open', targetId: 'q1' })).toEqual({ targetId: 'q1' })
  })

  it('cancel đóng hộp', () => {
    expect(confirmReducer({ targetId: 'q1' }, { type: 'cancel' })).toEqual(initialConfirmState)
  })
})

describe('canConfirm', () => {
  it('đối tượng vẫn hợp lệ: cho phép', () => {
    expect(canConfirm({ targetId: 'q1' }, 'q1')).toBe(true)
  })

  it('câu đã kết thúc khi hộp đang mở (activeId null): không cho phép', () => {
    expect(canConfirm({ targetId: 'q1' }, null)).toBe(false)
  })

  it('đã chuyển sang câu khác khi hộp đang mở: không cho phép', () => {
    expect(canConfirm({ targetId: 'q1' }, 'q2')).toBe(false)
  })

  it('hộp chưa mở: không cho phép', () => {
    expect(canConfirm(initialConfirmState, 'q1')).toBe(false)
  })
})
