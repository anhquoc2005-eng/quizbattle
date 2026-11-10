import { describeReward } from '../realtime/rewardMessages'
import type { PlayerReward } from '../types/game'

/** Khối thưởng ở màn kết thúc. Không có dữ liệu thưởng của người xem (ví dụ chủ phòng không chơi) thì không hiện gì. */
export function RewardSummary({ reward }: { reward: PlayerReward | undefined }) {
  if (!reward) {
    return null
  }

  const { awarded, summary, details, levelUpMessage } = describeReward(reward)
  return (
    <section className="grid gap-2 rounded-[14px] bg-raised px-4 py-3" aria-label="Thưởng của bạn">
      {awarded ? <p className="font-semibold tabular-nums">{summary}</p> : <p className="text-muted">{summary}</p>}
      {levelUpMessage && <p className="font-semibold text-positive">{levelUpMessage}</p>}
      {details.map((line) => (
        <p key={line} className="text-sm text-muted tabular-nums">
          {line}
        </p>
      ))}
    </section>
  )
}
