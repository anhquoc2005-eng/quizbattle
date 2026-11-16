import type { LeaderboardEntry } from '../types/game'
import { Confetti } from './Confetti'
import { PodiumStep } from './PodiumStep'
import { podiumSteps } from './podiumSteps'

/**
 * Bục vinh danh cuối trận: top 3 lộ dần hạng 3 → 2 → 1 rồi pháo giấy (CSS). Dùng chung cho màn chủ phòng và màn người chơi.
 * DOM theo thứ tự hạng 1, 2, 3 (đúng cho trình đọc màn hình); vị trí 2-1-3 trên màn do CSS. Không có ai thì không vẽ gì.
 */
export function Podium({ entries }: { entries: readonly LeaderboardEntry[] }) {
  const places = podiumSteps(entries)

  if (places.length === 0) {
    return null
  }

  return (
    <section className="podium" aria-label="Bục vinh danh">
      <ol className="podium-steps">
        {places.map(({ rank, entry }) => (
          <PodiumStep key={entry.playerId} rank={rank} entry={entry} />
        ))}
      </ol>
      <Confetti />
    </section>
  )
}
