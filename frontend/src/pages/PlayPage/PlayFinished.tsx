import { Link } from 'react-router'
import { Leaderboard } from '../../components/Leaderboard'
import { RewardSummary } from '../../components/RewardSummary'
import type { GameState } from '../../realtime/gameReducer'

export function PlayFinished({ state }: { state: GameState }) {
  const myReward = state.rewards?.find((r) => r.playerId === state.playerId)

  return (
    <section className="card grid gap-4">
      <h1>Trận đã kết thúc</h1>
      <RewardSummary reward={myReward} />
      <Leaderboard entries={state.finalLeaderboard ?? state.leaderboard} highlightPlayerId={state.playerId} />
      <Link className="btn btn-ghost justify-self-start" to="/">
        Về trang chủ
      </Link>
    </section>
  )
}
