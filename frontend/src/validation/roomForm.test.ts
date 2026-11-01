import { describe, expect, it } from 'vitest'
import { validateCreateRoomForm, validateQuestionTimeLimitSeconds, validateTopic } from './roomForm'

describe('validateTopic', () => {
  it('báo lỗi khi rỗng hoặc toàn khoảng trắng', () => {
    expect(validateTopic('')).toBeDefined()
    expect(validateTopic('   ')).toBeDefined()
  })

  it('báo lỗi khi vượt 200 ký tự', () => {
    expect(validateTopic('a'.repeat(201))).toBeDefined()
  })

  it('hợp lệ với chủ đề bình thường', () => {
    expect(validateTopic('Java OOP')).toBeUndefined()
  })
})

describe('validateQuestionTimeLimitSeconds', () => {
  it('báo lỗi ngoài khoảng 5-120', () => {
    expect(validateQuestionTimeLimitSeconds(4)).toBeDefined()
    expect(validateQuestionTimeLimitSeconds(121)).toBeDefined()
  })

  it('báo lỗi khi không phải số nguyên', () => {
    expect(validateQuestionTimeLimitSeconds(20.5)).toBeDefined()
  })

  it('hợp lệ trong khoảng 5-120', () => {
    expect(validateQuestionTimeLimitSeconds(20)).toBeUndefined()
  })
})

describe('validateCreateRoomForm', () => {
  it('trả về cả 2 lỗi khi cả 2 sai', () => {
    const errors = validateCreateRoomForm({ topic: '', questionTimeLimitSeconds: 1 })
    expect(errors.topic).toBeDefined()
    expect(errors.questionTimeLimitSeconds).toBeDefined()
  })
})
