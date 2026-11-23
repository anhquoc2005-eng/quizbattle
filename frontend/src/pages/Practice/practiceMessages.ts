import { ApiError, describeApiError } from '../../api/apiError'
import type { PracticeResult, PracticeXpReason } from '../../types/practice'

type ResultLike = Pick<PracticeResult, 'isCorrect' | 'correctIndex' | 'xpAwarded' | 'xpReason'>

function remainingText(remainingToday?: number): string {
  return remainingToday === undefined ? '' : `, còn ${remainingToday} hôm nay`
}

/** Thông điệp XP theo xpReason. remainingToday chỉ có ở phản hồi nộp, không có ở lastResult. */
export function describeXp(xpReason: PracticeXpReason | null, xpAwarded: number, remainingToday?: number): string {
  switch (xpReason) {
    case null:
      return `+${xpAwarded} XP${remainingText(remainingToday)}`
    case 'daily_cap_partial':
      return `+${xpAwarded} XP, chạm trần ngày${remainingText(remainingToday)}`
    case 'already_today':
      return 'câu này đã nhận XP hôm nay'
    case 'daily_cap':
      return 'hôm nay đã đạt trần XP'
    case 'wrong':
      return 'không có XP'
  }
}

/** Một dòng kết quả cho câu đã nộp. Sai thì nêu đáp án đúng (hiển thị 1-4, index lưu 0-3). */
export function describeResult(result: ResultLike, remainingToday?: number): string {
  if (!result.isCorrect) {
    return `Sai, đáp án đúng là lựa chọn ${result.correctIndex + 1}`
  }
  return `Đúng, ${describeXp(result.xpReason, result.xpAwarded, remainingToday)}`
}

export function describePreviousResult(result: ResultLike): string {
  return `Câu trước: ${describeResult(result)}`
}

export const SESSION_NOT_FOUND_MESSAGE = 'Không tìm thấy lượt ôn'

/** Thông báo lỗi khi nộp đáp án; 409 và lỗi mạng có câu riêng để người dùng biết phải làm gì tiếp. */
export function describePracticeError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Không kết nối được máy chủ. Bấm nộp đáp án để gửi lại lựa chọn này.'
  }
  if (error.status === 404) {
    return SESSION_NOT_FOUND_MESSAGE
  }
  if (error.status === 409) {
    return 'Câu này không còn là câu hiện tại, đã tải lại lượt ôn'
  }
  return describeApiError(error)
}
