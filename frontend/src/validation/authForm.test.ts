import { describe, expect, it } from 'vitest'
import { mapServerErrorsToFields, validateRegisterForm } from './authForm'

describe('mapServerErrorsToFields', () => {
  it('map đúng field, không phân biệt hoa thường', () => {
    const result = mapServerErrorsToFields({ Email: ['Email đã được sử dụng.'] }, ['email', 'password'] as const)

    expect(result).toEqual({ email: 'Email đã được sử dụng.' })
  })

  it('bỏ qua key lạ không khớp field nào', () => {
    const result = mapServerErrorsToFields(
      { Topic: ['Chủ đề không được để trống.'] },
      ['email', 'password'] as const,
    )

    expect(result).toEqual({})
  })

  it('trả về object rỗng khi không có errors', () => {
    expect(mapServerErrorsToFields(undefined, ['email'] as const)).toEqual({})
  })
})

describe('validateRegisterForm', () => {
  it('báo lỗi email sai định dạng', () => {
    const errors = validateRegisterForm({ displayName: 'Alice', email: 'khong-phai-email', password: 'password123' })

    expect(errors.email).toBeDefined()
    expect(errors.password).toBeUndefined()
    expect(errors.displayName).toBeUndefined()
  })

  it('báo lỗi mật khẩu dưới 8 ký tự', () => {
    const errors = validateRegisterForm({ displayName: 'Alice', email: 'alice@example.com', password: '1234567' })

    expect(errors.password).toBeDefined()
  })

  it('báo lỗi tên hiển thị toàn khoảng trắng', () => {
    const errors = validateRegisterForm({ displayName: '   ', email: 'alice@example.com', password: 'password123' })

    expect(errors.displayName).toBeDefined()
  })

  it('không có lỗi khi dữ liệu hợp lệ', () => {
    const errors = validateRegisterForm({ displayName: 'Alice', email: 'alice@example.com', password: 'password123' })

    expect(errors).toEqual({ displayName: undefined, email: undefined, password: undefined })
  })
})
