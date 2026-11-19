/** Khớp AiQuotaResponse của backend (GET /api/ai-quota). Không có số lượt global: chỉ biết đã hết hay chưa. */
export interface AiQuota {
  dailyLimit: number
  used: number
  remaining: number
  globalExhausted: boolean
  /** ISO 8601 offset +07:00: 00:00 giờ Việt Nam của ngày kế tiếp. Chỉ dùng để biết có hiện câu "làm mới" hay không. */
  resetAt: string
}
