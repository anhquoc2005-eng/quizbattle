import type { GameError } from '../types/game'

/**
 * Chỉ xóa sessionToken đã lưu khi chính JoinRoom thất bại vì phiên sai/không tồn tại (unauthorized/not_found).
 * Lỗi tạm thời (conflict, unknown - DB chập chờn, mất mạng giữa chừng...) KHÔNG được xóa: nếu xóa nhầm,
 * người chơi bị đẩy về /join rồi gặp 409 vì phòng không còn Lobby nữa - mất quyền vào lại trận đang diễn ra.
 */
export function shouldDiscardSession(error: GameError): boolean {
  return error.method === 'JoinRoom' && (error.code === 'unauthorized' || error.code === 'not_found')
}
