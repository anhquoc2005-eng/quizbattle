import { describe, expect, it } from 'vitest'
import { validateJoinForm, validateRoomCode } from './joinForm'

describe('validateRoomCode', () => {
  it('báo lỗi khi không đúng 6 ký tự', () => {
    expect(validateRoomCode('ABC')).toBeDefined()
    expect(validateRoomCode('ABCDEFG')).toBeDefined()
    expect(validateRoomCode('')).toBeDefined()
  })

  it('hợp lệ với đúng 6 ký tự', () => {
    expect(validateRoomCode('K7Q2MX')).toBeUndefined()
  })
})

describe('validateJoinForm', () => {
  it('báo đủ lỗi khi cả mã phòng và tên hiển thị đều sai', () => {
    const errors = validateJoinForm({ code: '123', displayName: '' })
    expect(errors.code).toBeDefined()
    expect(errors.displayName).toBeDefined()
  })

  it('không có lỗi khi mọi field hợp lệ', () => {
    const errors = validateJoinForm({ code: 'K7Q2MX', displayName: 'Alice' })
    expect(errors.code).toBeUndefined()
    expect(errors.displayName).toBeUndefined()
  })
})
