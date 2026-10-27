import { describe, expect, it } from 'vitest'
import { ApiError, parseProblemDetails } from './apiError'

describe('parseProblemDetails', () => {
  it('trả về null khi body không phải object', () => {
    expect(parseProblemDetails(null)).toBeNull()
    expect(parseProblemDetails('lỗi')).toBeNull()
    expect(parseProblemDetails(500)).toBeNull()
  })

  it('trả về null khi object không có trường nào của ProblemDetails', () => {
    expect(parseProblemDetails({ status: 'degraded', database: 'down' })).toBeNull()
  })

  it('đọc đúng các trường của ProblemDetails', () => {
    const problem = parseProblemDetails({
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      title: 'Dữ liệu không hợp lệ.',
      status: 400,
      errors: { topic: ['Chủ đề không được để trống.'] },
    })

    expect(problem).toEqual({
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      title: 'Dữ liệu không hợp lệ.',
      status: 400,
      detail: undefined,
      errors: { topic: ['Chủ đề không được để trống.'] },
    })
  })

  it('bỏ qua errors sai định dạng', () => {
    const problem = parseProblemDetails({ title: 'Lỗi', errors: { topic: 'không phải mảng' } })

    expect(problem?.errors).toBeUndefined()
  })
})

describe('ApiError', () => {
  it('dùng title của ProblemDetails làm message', () => {
    expect(new ApiError(404, { title: 'Không tìm thấy phòng.' }).message).toBe('Không tìm thấy phòng.')
  })

  it('có message mặc định khi không có ProblemDetails', () => {
    expect(new ApiError(503, null).message).toBe('Yêu cầu thất bại (HTTP 503)')
  })
})
