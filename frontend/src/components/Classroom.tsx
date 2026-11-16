import type { PlayerPresence } from '../realtime/gameReducer'
import { ClassroomDesk } from './ClassroomDesk'
import { classroomDensity } from './classroomDensity'
import { deskStatus, type ClassroomRound } from './deskStatus'

interface ClassroomProps {
  players: PlayerPresence[]
  /** Dữ liệu câu hỏi hiện tại để vẽ giơ tay / ✓✗; bỏ trống (sảnh) thì mọi bàn ở trạng thái thường. */
  round?: ClassroomRound
}

/**
 * Lớp học ở màn chủ phòng: mỗi người chơi một bàn (nhân vật + tên). Hiển thị ĐÚNG THỨ TỰ `players` nhận được (server sắp theo
 * JoinedAt rồi Id, người vào sau nối cuối) - KHÔNG sắp theo điểm hay bảng xếp hạng để bàn không đổi chỗ giữa trận.
 * Người rớt mạng (online = false) vẫn giữ bàn; hiển thị trạng thái online nằm ngoài phạm vi.
 */
export function Classroom({ players, round }: ClassroomProps) {
  if (players.length === 0) {
    return <p className="text-muted">Chưa có ai vào phòng.</p>
  }

  const density = classroomDensity(players.length)

  return (
    <ul className="classroom" data-density={density}>
      {players.map((player) => (
        <ClassroomDesk key={player.playerId} player={player} density={density} status={deskStatus(player.playerId, round)} />
      ))}
    </ul>
  )
}
