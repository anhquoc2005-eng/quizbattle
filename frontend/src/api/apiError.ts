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
  }
}

/** Câu lỗi tiếng Việt chung cho các khối gọi API ngoài 400 (400 hiển thị dưới đúng ô bằng mapServerErrorsToFields). */
export function describeApiError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Không kết nối được máy chủ.'
  }

  switch (error.status) {
    case 429:
      return 'Bạn đã sinh câu hỏi quá nhiều, thử lại sau.'
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
