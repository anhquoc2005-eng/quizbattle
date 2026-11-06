import { describe, expect, it } from 'vitest'
import { shouldShowGenerateError } from './generateError'

describe('shouldShowGenerateError', () => {
  it('không có lỗi: không hiện', () => {
    expect(shouldShowGenerateError(undefined, 3)).toBe(false)
  })

  it('số câu không đổi từ lúc lỗi: hiện', () => {
    expect(shouldShowGenerateError({ message: 'AI bận', atCount: 3 }, 3)).toBe(true)
  })

  it('đã thêm câu thủ công sau lỗi: ẩn', () => {
    expect(shouldShowGenerateError({ message: 'AI bận', atCount: 3 }, 4)).toBe(false)
  })

  it('danh sách chưa tải lúc lỗi và vẫn chưa tải: hiện', () => {
    expect(shouldShowGenerateError({ message: 'AI bận', atCount: undefined }, undefined)).toBe(true)
  })
})
