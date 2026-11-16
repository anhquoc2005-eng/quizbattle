import type { LeaderboardEntry } from '../types/game'
import { Avatar } from './Avatar'

/** Một bậc: nhân vật + tên + điểm trên khối bục cao theo hạng. Thứ tự lộ dần và vị trí 2-1-3 do CSS quyết định theo data-rank. */
export function PodiumStep({ rank, entry }: { rank: number; entry: LeaderboardEntry }) {
  return (
    <li className="podium-place" data-rank={rank}>
      <span className="sr-only">Hạng {rank}: </span>
      <div className="podium-person">
        <span className="podium-avatar">
          <Avatar avatar={entry.avatar} size="fill" />
        </span>
        <span className="podium-name" title={entry.displayName}>
          {entry.displayName}
        </span>
        <span className="podium-score">{entry.score}</span>
      </div>
      <div className="podium-block" aria-hidden="true">
        {rank}
      </div>
    </li>
  )
}
