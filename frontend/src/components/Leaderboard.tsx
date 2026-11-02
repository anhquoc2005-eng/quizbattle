import type { LeaderboardEntry } from '../types/game'

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
            className={`flex items-center justify-between gap-3 rounded-[14px] px-3 py-2 ${isSelf ? 'bg-raised' : ''}`}
          >
            <span className="flex items-center gap-3">
              <span className="w-6 text-right tabular-nums text-faint">{entry.rank}</span>
              <span className={isSelf ? 'font-semibold' : undefined}>{entry.displayName}</span>
            </span>
            <span className="tabular-nums text-muted">{entry.score}</span>
          </li>
        )
      })}
    </ol>
  )
}
