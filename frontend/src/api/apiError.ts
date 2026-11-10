import type { ProblemDetails } from '../types/problemDetails'

/** Lỗi khi backend trả về mã HTTP không thành công. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.title ?? `Yêu cầu thất bại (HTTP ${status})`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

/** Kiểm tra body lỗi có đúng dạng ProblemDetails không. Không đúng thì trả về null. */
export function parseProblemDetails(body: unknown): ProblemDetails | null {
  if (typeof body !== 'object' || body === null) {
    return null
  }

  const candidate = body as Record<string, unknown>
  const hasProblemField =
    typeof candidate.title === 'string' ||
    typeof candidate.detail === 'string' ||
    typeof candidate.type === 'string'

  if (!hasProblemField) {
    return null
  }

  return {
    type: typeof candidate.type === 'string' ? candidate.type : undefined,
    title: typeof candidate.title === 'string' ? candidate.title : undefined,
    status: typeof candidate.status === 'number' ? candidate.status : undefined,
    detail: typeof candidate.detail === 'string' ? candidate.detail : undefined,
    errors: isValidationErrors(candidate.errors) ? candidate.errors : undefined,
    code: typeof candidate.code === 'string' ? candidate.code : undefined,
  }
}

/**
 * 401 khi vào phòng kèm Authorization: 'expired' nếu token hết hạn hoặc không hợp lệ (người dùng phải chọn đăng nhập lại
 * hoặc vào làm khách, không được tự thành khách); null cho mọi lỗi khác.
 */
export function describeJoinAuthError(problem: ProblemDetails | null): 'expired' | null {
  return problem?.code === 'token_expired' || problem?.code === 'token_invalid' ? 'expired' : null
}

/**
 * Câu lỗi tiếng Việt chung cho các khối gọi API ngoài 400 (400 hiển thị dưới đúng ô bằng mapServerErrorsToFields).
 * overrides cho phép 1 màn cụ thể thay câu mặc định của 1 mã trạng thái (ví dụ màn sinh câu hỏi vẫn
 * muốn câu 429 nói rõ "sinh câu hỏi quá nhiều" thay vì câu chung chung).
 */
export function describeApiError(error: unknown, overrides?: Partial<Record<number, string>>): string {
  if (!(error instanceof ApiError)) {
    return 'Không kết nối được máy chủ.'
  }

  if (overrides?.[error.status]) {
    return overrides[error.status]!
  }

  switch (error.status) {
    case 429:
      return 'Bạn thao tác quá nhanh, thử lại sau ít phút.'
    case 502:
      return 'AI trả kết quả không dùng được, hãy thử lại.'
    case 503:
      return 'Dịch vụ AI đang bận, thử lại sau ít phút.'
    default:
      return error.problem?.title ?? 'Có lỗi xảy ra.'
  }
}

function isValidationErrors(value: unknown): value is Record<string, string[]> {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  return Object.values(value).every(
    (messages) => Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
  )
}
