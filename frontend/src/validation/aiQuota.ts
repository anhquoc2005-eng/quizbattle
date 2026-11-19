import type { AiQuota } from '../types/aiQuota'

export type GenerateBlock = 'none' | 'user' | 'global'

export const AI_DAILY_LIMIT_USER = 'AI_DAILY_LIMIT_USER'
export const AI_DAILY_LIMIT_GLOBAL = 'AI_DAILY_LIMIT_GLOBAL'
export const AI_RATE_LIMIT = 'AI_RATE_LIMIT'

// Cố định, KHÔNG đổi theo múi giờ trình duyệt: bộ đếm làm mới lúc 00:00 giờ Việt Nam bất kể người dùng ở đâu.
const RESET_SENTENCE = 'làm mới lúc 00:00 (giờ Việt Nam)'

/**
 * Vì sao nút Sinh phải tắt. Hết cả lượt của mình lẫn của hệ thống thì báo 'global' (khớp server: kiểm global trước,
 * trả AI_DAILY_LIMIT_GLOBAL).
 */
export function getGenerateBlock(quota: AiQuota): GenerateBlock {
  if (quota.globalExhausted) {
    return 'global'
  }

  return quota.remaining <= 0 ? 'user' : 'none'
}

export function blockToCode(block: Exclude<GenerateBlock, 'none'>): string {
  return block === 'global' ? AI_DAILY_LIMIT_GLOBAL : AI_DAILY_LIMIT_USER
}

/**
 * Thông báo cho 3 mã 429 của sinh câu hỏi AI; mã khác trả undefined để nơi gọi dùng câu chung.
 * `resetAt` chỉ dùng để biết server có cho biết thời điểm làm mới không (có thì hiện câu cố định ở trên); giá trị của nó
 * không bao giờ được định dạng lại theo múi giờ trình duyệt.
 */
export function describeAiLimit(code: string | undefined, resetAt: string | undefined): string | undefined {
  const resetPart = resetAt ? `, ${RESET_SENTENCE}.` : '.'

  switch (code) {
    case AI_DAILY_LIMIT_USER:
      return `Bạn đã dùng hết lượt AI hôm nay${resetPart}`
    case AI_DAILY_LIMIT_GLOBAL:
      return `Hệ thống AI hôm nay đã hết lượt${resetPart}`
    case AI_RATE_LIMIT:
      return 'Bạn sinh câu hỏi quá nhanh, thử lại sau ít phút.'
    default:
      return undefined
  }
}
