import type { GameError } from '../types/game'

/**
 * Chỉ xóa sessionToken đã lưu khi chính JoinRoom thất bại vì phiên sai/không tồn tại (unauthorized/not_found).
 * Lỗi tạm thời (conflict, unknown - DB chập chờn, mất mạng giữa chừng...) KHÔNG được xóa: nếu xóa nhầm,
 * người chơi bị đẩy về /join rồi gặp 409 vì phòng không còn Lobby nữa - mất quyền vào lại trận đang diễn ra.
 */
export function shouldDiscardSession(error: GameError): boolean {
  return error.method === 'JoinRoom' && (error.code === 'unauthorized' || error.code === 'not_found')
}

/**
 * EndQuestion/NextQuestion bị conflict nghĩa là server (timer) vừa tự chuyển trước khi bấm tới nơi:
 * trạng thái đúng sẽ tới qua sự kiện, nên không hiện lỗi cho chủ phòng.
 */
export function shouldIgnoreError(error: GameError): boolean {
  return (error.method === 'EndQuestion' || error.method === 'NextQuestion') && error.code === 'conflict'
}
