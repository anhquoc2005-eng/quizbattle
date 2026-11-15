import type { PlayerPresence } from '../realtime/gameReducer'
import { Avatar } from './Avatar'
import { LevelBadge } from './LevelBadge'

/** Danh sách người chơi kèm trạng thái online/offline - dùng chung cho màn người chơi và màn điều khiển. */
export function PlayerList({ players }: { players: PlayerPresence[] }) {
  if (players.length === 0) {
    return <p className="text-muted">Chưa có ai vào phòng.</p>
  }

  return (
    <ul className="grid list-none gap-1.5 p-0">
      {players.map((player) => (
        <li key={player.playerId} className="flex items-center gap-2">
          <span className={`size-2 shrink-0 rounded-full ${player.online ? 'bg-positive' : 'bg-faint'}`} aria-hidden="true" />
          <Avatar avatar={player.avatar} size="sm" />
          <span className={player.online ? undefined : 'text-faint'}>{player.displayName}</span>
          <LevelBadge level={player.level} />
          {!player.online && <span className="text-xs text-faint">(mất kết nối)</span>}
        </li>
      ))}
    </ul>
  )
}
