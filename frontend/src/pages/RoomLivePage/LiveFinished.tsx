import { Link } from 'react-router'
import { Leaderboard } from '../../components/Leaderboard'
import type { GameState } from '../../realtime/gameReducer'

export function LiveFinished({ state }: { state: GameState }) {
  return (
    <section className="card grid gap-4">
      <h1>Trận đã kết thúc</h1>
      <Leaderboard entries={state.finalLeaderboard ?? state.leaderboard} />
      <Link className="btn btn-ghost justify-self-start" to="/dashboard">
        Về Phòng của tôi
      </Link>
    </section>
  )
}
