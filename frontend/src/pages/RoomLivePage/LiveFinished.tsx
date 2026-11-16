import { Link } from 'react-router'
import { Leaderboard } from '../../components/Leaderboard'
import { Podium } from '../../components/Podium'
import type { GameState } from '../../realtime/gameReducer'

export function LiveFinished({ state }: { state: GameState }) {
  const ranking = state.finalLeaderboard ?? state.leaderboard

  return (
    <section className="card grid gap-4">
      <h1>Trận đã kết thúc</h1>
      <Podium entries={ranking} />
      <Leaderboard entries={ranking} />
      <Link className="btn btn-ghost justify-self-start" to="/dashboard">
        Về Phòng của tôi
      </Link>
    </section>
  )
}
