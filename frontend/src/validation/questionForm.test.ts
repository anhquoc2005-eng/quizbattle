import { describe, expect, it } from 'vitest'
import {
  validateCorrectIndex,
  validateGenerateCount,
  validateOptions,
  validateQuestionForm,
  validateQuestionText,
} from './questionForm'

describe('validateQuestionText', () => {
  it('báo lỗi khi rỗng hoặc vượt 500 ký tự', () => {
    expect(validateQuestionText('')).toBeDefined()
    expect(validateQuestionText('a'.repeat(501))).toBeDefined()
  })

  it('hợp lệ với nội dung bình thường', () => {
    expect(validateQuestionText('Java dùng từ khóa gì để kế thừa?')).toBeUndefined()
  })
})

describe('validateOptions', () => {
  it('báo lỗi khi không đúng 4 đáp án', () => {
    expect(validateOptions(['A', 'B', 'C'])).toBeDefined()
  })

  it('báo lỗi khi có đáp án rỗng', () => {
    expect(validateOptions(['A', '', 'C', 'D'])).toBeDefined()
  })

  it('báo lỗi khi 2 đáp án trùng nhau (không phân biệt hoa thường)', () => {
    expect(validateOptions(['extends', 'Extends', 'implements', 'super'])).toBeDefined()
  })

  it('hợp lệ với 4 đáp án khác nhau', () => {
    expect(validateOptions(['extends', 'implements', 'inherits', 'super'])).toBeUndefined()
  })
})

describe('validateCorrectIndex', () => {
  it('báo lỗi khi chưa chọn hoặc ngoài khoảng 0-3', () => {
    expect(validateCorrectIndex(null)).toBeDefined()
    expect(validateCorrectIndex(4)).toBeDefined()
    expect(validateCorrectIndex(-1)).toBeDefined()
  })

  it('hợp lệ trong khoảng 0-3', () => {
    expect(validateCorrectIndex(0)).toBeUndefined()
    expect(validateCorrectIndex(3)).toBeUndefined()
  })
})

describe('validateGenerateCount', () => {
  it('báo lỗi ngoài khoảng 5-20', () => {
    expect(validateGenerateCount(4)).toBeDefined()
    expect(validateGenerateCount(21)).toBeDefined()
  })

  it('hợp lệ trong khoảng 5-20', () => {
    expect(validateGenerateCount(5)).toBeUndefined()
    expect(validateGenerateCount(20)).toBeUndefined()
  })
})

describe('validateQuestionForm', () => {
  it('trả về đủ lỗi khi mọi field đều sai', () => {
    const errors = validateQuestionForm({ text: '', options: ['A', 'A', 'B', 'C'], correctIndex: null })
    expect(errors.text).toBeDefined()
    expect(errors.options).toBeDefined()
    expect(errors.correctIndex).toBeDefined()
  })

  it('không có lỗi khi mọi field hợp lệ', () => {
    const errors = validateQuestionForm({
      text: 'Từ khóa nào dùng để kế thừa?',
      options: ['extends', 'implements', 'inherits', 'super'],
      correctIndex: 0,
    })
    expect(errors.text).toBeUndefined()
    expect(errors.options).toBeUndefined()
    expect(errors.correctIndex).toBeUndefined()
  })
})
