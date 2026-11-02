import type { GameError } from '../types/game'
import type { ConnectionStatus } from './GameConnectionProvider'
import type { GameState } from './gameReducer'

export type GameScreen = 'connecting' | 'disconnected' | 'joinError' | 'game'

/**
 * Chọn màn hiển thị cho /play/:code và /rooms/:code/live. Thứ tự quan trọng:
 * - Mất kết nối hẳn: luôn hiện nút "Kết nối lại", kể cả khi chưa từng nhận RoomState.
 * - JoinRoom lỗi mà chưa có RoomState: server sẽ không bao giờ gửi RoomState nữa - phải hiện lỗi, không
 *   được rơi xuống nhánh "Đang kết nối…" (kẹt mãi).
 */
export function getScreen(connectionStatus: ConnectionStatus, state: Pick<GameState, 'status'>, lastError: GameError | null): GameScreen {
  if (connectionStatus === 'disconnected') return 'disconnected'
  if (state.status === null && lastError?.method === 'JoinRoom') return 'joinError'
  if (connectionStatus === 'connecting' || state.status === null) return 'connecting'
  return 'game'
}
