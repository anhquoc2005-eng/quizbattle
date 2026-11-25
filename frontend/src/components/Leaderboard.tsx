import type { LeaderboardEntry } from '../types/game'
import { Avatar } from './Avatar'
import { LevelBadge } from './LevelBadge'

/** Bảng xếp hạng dùng chung cho màn người chơi và màn điều khiển - làm nổi dòng của chính mình nếu truyền highlightPlayerId. */
export function Leaderboard({ entries, highlightPlayerId }: { entries: LeaderboardEntry[]; highlightPlayerId?: string | null }) {
  if (entries.length === 0) {
    return <p className="text-muted">Chưa có ai ghi điểm.</p>
  }

  return (
    <ol className="grid list-none gap-1.5 p-0">
      {entries.map((entry) => {
        const isSelf = entry.playerId === highlightPlayerId
        return (
          <li
            key={entry.playerId}
            className={`flex min-w-0 items-center justify-between gap-3 rounded-[14px] px-3 py-2 ${isSelf ? 'bg-raised' : ''}`}
          >
            <span className="flex min-w-0 items-center gap-3">
              <span className="w-6 shrink-0 text-right tabular-nums text-faint">{entry.rank}</span>
              <span className="shrink-0">
                <Avatar avatar={entry.avatar} size="sm" />
              </span>
              <span className={`name-truncate ${isSelf ? 'font-semibold' : ''}`} title={entry.displayName}>
                {entry.displayName}
              </span>
              <span className="shrink-0">
                <LevelBadge level={entry.level} />
              </span>
            </span>
            <span className="shrink-0 tabular-nums text-muted">{entry.score}</span>
          </li>
        )
      })}
    </ol>
  )
}
