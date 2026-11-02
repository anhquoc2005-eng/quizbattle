import { PlayerList } from '../../components/PlayerList'
import type { GameState } from '../../realtime/gameReducer'

export function PlayLobby({ state }: { state: GameState }) {
  return (
    <section className="card grid gap-4">
      <h1>Đang chờ chủ phòng bắt đầu</h1>
      <PlayerList players={state.players} />
    </section>
  )
}
