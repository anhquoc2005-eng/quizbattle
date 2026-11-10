import type { GameError } from '../types/game'

/**
 * Chỉ xóa sessionToken đã lưu khi chính JoinRoom thất bại vì phiên sai/không tồn tại (unauthorized/not_found).
 * Lỗi tạm thời (conflict, unknown - DB chập chờn, mất mạng giữa chừng...) KHÔNG được xóa: nếu xóa nhầm,
 * người chơi bị đẩy về /join rồi gặp 409 vì phòng không còn Lobby nữa - mất quyền vào lại trận đang diễn ra.
 */
export function shouldDiscardSession(error: GameError): boolean {
  return isSessionReplaced(error) || (error.method === 'JoinRoom' && (error.code === 'unauthorized' || error.code === 'not_found'))
}

/**
 * Cùng tài khoản vừa vào lại phòng từ thiết bị khác: token của thiết bị này đã vô hiệu (mọi method, kể cả SubmitAnswer),
 * nên phải xóa phiên đã lưu và dừng tự kết nối lại - kết nối lại với token cũ chỉ bị từ chối tiếp.
 */
export function isSessionReplaced(error: GameError): boolean {
  return error.code === 'session_replaced'
}

/**
 * EndQuestion/NextQuestion bị conflict nghĩa là server (timer) vừa tự chuyển trước khi bấm tới nơi:
 * trạng thái đúng sẽ tới qua sự kiện, nên không hiện lỗi cho chủ phòng.
 */
export function shouldIgnoreError(error: GameError): boolean {
  return (error.method === 'EndQuestion' || error.method === 'NextQuestion') && error.code === 'conflict'
}
