/** Trạng thái hộp xác nhận: nhớ đối tượng (id) đang được hỏi, để hộp chỉ mở/Đồng ý có hiệu lực khi đối tượng đó còn hợp lệ. */
export interface ConfirmState {
  targetId: string | null
}

export type ConfirmAction = { type: 'open'; targetId: string } | { type: 'cancel' }

export const initialConfirmState: ConfirmState = { targetId: null }

export function confirmReducer(_state: ConfirmState, action: ConfirmAction): ConfirmState {
  return action.type === 'open' ? { targetId: action.targetId } : initialConfirmState
}

/**
 * Hộp đang mở cho đúng đối tượng còn hợp lệ. activeId là id hiện còn hợp lệ (null nếu không còn,
 * ví dụ câu đã kết thúc). Dùng cả để quyết định hiển thị hộp (tự đóng) lẫn cho phép gửi lệnh khi Đồng ý.
 */
export function canConfirm(state: ConfirmState, activeId: string | null): boolean {
  return state.targetId !== null && state.targetId === activeId
}
