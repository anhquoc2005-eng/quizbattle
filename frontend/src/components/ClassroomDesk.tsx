import { memo } from 'react'
import type { PlayerPresence } from '../realtime/gameReducer'
import { Avatar } from './Avatar'
import { DESK_AVATAR_SIZE, type ClassroomDensity } from './classroomDensity'

/**
 * Một bàn học: nhân vật + tên. memo + props phẳng (player giữ nguyên tham chiếu khi chỉ người khác đổi) để lớp 50 người
 * không vẽ lại 50 nhân vật mỗi lần có sự kiện; ảnh nhân vật đã nằm trong bộ nhớ đệm của avatarRenderer.
 */
export const ClassroomDesk = memo(function ClassroomDesk({ player, density }: { player: PlayerPresence; density: ClassroomDensity }) {
  return (
    <li className="desk">
      <Avatar avatar={player.avatar} size={DESK_AVATAR_SIZE[density]} />
      <span className="desk-name" title={player.displayName}>
        {player.displayName}
      </span>
    </li>
  )
})
