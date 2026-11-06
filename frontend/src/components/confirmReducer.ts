/** Trạng thái hộp xác nhận: nhớ đối tượng (id) đang được hỏi, để Đồng ý chỉ có hiệu lực nếu đối tượng đó còn hợp lệ. */
export interface ConfirmState {
  targetId: string | null
}

export type ConfirmAction =
  | { type: 'open'; targetId: string }
  | { type: 'cancel' }
  /** activeId: id đối tượng còn hợp lệ ở thời điểm này (null nếu không còn, ví dụ câu đã kết thúc). */
  | { type: 'sync'; activeId: string | null }

export const initialConfirmState: ConfirmState = { targetId: null }

export function confirmReducer(state: ConfirmState, action: ConfirmAction): ConfirmState {
  switch (action.type) {
    case 'open':
      return { targetId: action.targetId }
    case 'cancel':
      return initialConfirmState
    case 'sync':
      return state.targetId !== null && state.targetId !== action.activeId ? initialConfirmState : state
  }
}

/** Đồng ý chỉ được gửi lệnh khi hộp đang mở cho đúng đối tượng còn hợp lệ. */
export function canConfirm(state: ConfirmState, activeId: string | null): boolean {
  return state.targetId !== null && state.targetId === activeId
}
