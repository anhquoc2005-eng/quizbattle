import { describe, expect, it } from 'vitest'
import { ApiError, describeApiError, describeJoinAuthError, parseProblemDetails } from './apiError'

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

describe('describeApiError', () => {
  it('429 mặc định là câu chung chung', () => {
    expect(describeApiError(new ApiError(429, null))).toBe('Bạn thao tác quá nhanh, thử lại sau ít phút.')
  })

  it('overrides thay được câu mặc định theo ngữ cảnh', () => {
    const message = describeApiError(new ApiError(429, null), { 429: 'Bạn đã sinh câu hỏi quá nhiều, thử lại sau.' })
    expect(message).toBe('Bạn đã sinh câu hỏi quá nhiều, thử lại sau.')
  })

  it('overrides không ảnh hưởng mã trạng thái khác', () => {
    const message = describeApiError(new ApiError(503, null), { 429: 'Câu riêng cho 429.' })
    expect(message).toBe('Dịch vụ AI đang bận, thử lại sau ít phút.')
  })

  it('lỗi không phải ApiError (mất mạng) trả về câu chung', () => {
    expect(describeApiError(new TypeError('Failed to fetch'))).toBe('Không kết nối được máy chủ.')
  })
})

describe('parseProblemDetails: code', () => {
  it('giữ trường mở rộng code (401 token_expired) để client phân biệt nguyên nhân', () => {
    const problem = parseProblemDetails({ title: 'Phiên đăng nhập đã hết hạn.', status: 401, code: 'token_expired' })

    expect(problem?.code).toBe('token_expired')
  })

  it('bỏ qua code không phải chuỗi', () => {
    expect(parseProblemDetails({ title: 'x', code: 5 })?.code).toBeUndefined()
  })
})

describe('describeJoinAuthError', () => {
  it('describeJoinAuthError_TokenExpired_ReturnsExpired', () => {
    expect(describeJoinAuthError({ status: 401, code: 'token_expired' })).toBe('expired')
  })

  it('describeJoinAuthError_TokenInvalid_ReturnsExpired', () => {
    expect(describeJoinAuthError({ status: 401, code: 'token_invalid' })).toBe('expired')
  })

  it('describeJoinAuthError_Other401_ReturnsNull', () => {
    expect(describeJoinAuthError({ status: 401, title: 'Sai mật khẩu.' })).toBeNull()
    expect(describeJoinAuthError({ status: 401, code: 'khac' })).toBeNull()
  })

  it('không có problem thì null', () => {
    expect(describeJoinAuthError(null)).toBeNull()
  })
})
