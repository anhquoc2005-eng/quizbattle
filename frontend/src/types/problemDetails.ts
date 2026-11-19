/** Format lỗi chung backend trả về (RFC 9457 ProblemDetails). */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
  /** Mã lỗi máy đọc được (vd token_expired) - trường mở rộng của backend, không phải chuẩn RFC 9457. */
  code?: string
  /** Chỉ có ở 429 hết lượt AI theo ngày (AI_DAILY_LIMIT_*): ISO 8601 offset +07:00, thời điểm bộ đếm làm mới. */
  resetAt?: string
}
