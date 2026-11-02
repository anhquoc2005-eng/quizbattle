import { useState } from 'react'
import { Link } from 'react-router'
import { PlayerList } from '../../components/PlayerList'
import { RoomCodeBoxes } from '../../components/RoomCodeBoxes'
import type { GameState } from '../../realtime/gameReducer'
import { useGameConnection } from '../../realtime/useGameConnection'

export function LiveLobby({ state }: { state: GameState }) {
  const { startGame } = useGameConnection()
  const [starting, setStarting] = useState(false)
  const code = state.code ?? ''

  async function handleStart() {
    setStarting(true)
    await startGame()
    setStarting(false)
  }

  return (
    <section className="card grid gap-4">
      <div className="grid gap-3">
        <h1>Phòng chờ</h1>
        <RoomCodeBoxes chars={code.split('')} label={`Mã phòng ${code}`} />
        <p className="text-muted">
          Người chơi vào tại{' '}
          <Link className="underline" to={`/join?code=${code}`}>
            /join?code={code}
          </Link>
        </p>
      </div>

      <div>
        <h2 className="mb-2">Người chơi ({state.players.length})</h2>
        <PlayerList players={state.players} />
      </div>

      <button
        className="btn btn-primary justify-self-start"
        type="button"
        onClick={handleStart}
        disabled={state.players.length === 0 || starting}
      >
        {starting ? 'Đang bắt đầu…' : 'Bắt đầu'}
      </button>
    </section>
  )
}
