import { memo } from 'react'
import type { PlayerPresence } from '../realtime/gameReducer'
import { Avatar } from './Avatar'
import { DeskBadge } from './DeskBadge'
import { DESK_AVATAR_SIZE, type ClassroomDensity } from './classroomDensity'
import type { DeskStatus } from './deskStatus'

interface ClassroomDeskProps {
  player: PlayerPresence
  density: ClassroomDensity
  status: DeskStatus
}

/**
 * Một bàn học: nhân vật + tên, huy hiệu trạng thái phủ lên nhân vật. memo + props phẳng (player giữ nguyên tham chiếu khi chỉ
 * người khác đổi, status là chuỗi) để lớp 50 người chỉ vẽ lại ĐÚNG bàn đổi trạng thái; ảnh nhân vật nằm trong bộ nhớ đệm
 * của avatarRenderer.
 */
export const ClassroomDesk = memo(function ClassroomDesk({ player, density, status }: ClassroomDeskProps) {
  return (
    <li className="desk" data-status={status}>
      <span className="desk-figure">
        <Avatar avatar={player.avatar} size={DESK_AVATAR_SIZE[density]} />
        {status !== 'idle' && <DeskBadge status={status} />}
      </span>
      <span className="desk-name" title={player.displayName}>
        {player.displayName}
      </span>
    </li>
  )
})
