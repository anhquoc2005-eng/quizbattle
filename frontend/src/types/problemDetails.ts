/** Format lỗi chung backend trả về (RFC 9457 ProblemDetails). */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
