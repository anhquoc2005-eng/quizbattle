/** Trạng thái một bàn học: thường, giơ tay (đã trả lời, câu đang chạy), ✓ / ✗ (câu đã kết thúc). */
export type DeskStatus = 'idle' | 'raised' | 'correct' | 'wrong'

/** Dữ liệu một câu hỏi mà lớp học cần để suy ra trạng thái bàn - toàn bộ do server gửi, client không tự chấm. */
export interface ClassroomRound {
  ended: boolean
  answeredPlayerIds: readonly string[]
  /** null khi chưa nhận được (chủ phòng vào lại đúng 6 giây hiển thị kết quả): bàn giữ trạng thái thường, không đoán đúng/sai. */
  correctPlayerIds: readonly string[] | null
}

/**
 * Không có round (sảnh) → thường. Câu đang chạy: đã trả lời thì giơ tay (KHÔNG biết chọn đáp án nào).
 * Câu đã kết thúc: đúng → ✓; mọi người còn lại, kể cả không trả lời → ✗.
 */
export function deskStatus(playerId: string, round: ClassroomRound | undefined): DeskStatus {
  if (round === undefined) return 'idle'

  if (round.ended) {
    if (round.correctPlayerIds === null) return 'idle'
    return round.correctPlayerIds.includes(playerId) ? 'correct' : 'wrong'
  }

  return round.answeredPlayerIds.includes(playerId) ? 'raised' : 'idle'
}
