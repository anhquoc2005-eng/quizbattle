import { Link, useParams } from 'react-router'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import { GameConnectionProvider } from '../../realtime/GameConnectionProvider'
import { useGameConnection } from '../../realtime/useGameConnection'
import { LiveFinished } from './LiveFinished'
import { LiveLobby } from './LiveLobby'
import { LiveQuestion } from './LiveQuestion'

export function RoomLivePage() {
  const params = useParams<{ code: string }>()
  const code = (params.code ?? '').toUpperCase()

  return (
    <GameConnectionProvider code={code}>
      <RoomLivePageContent />
    </GameConnectionProvider>
  )
}

function RoomLivePageContent() {
  const { user, logout } = useAuth()
  const { state, connectionStatus, lastError, clearError, reconnect } = useGameConnection()

  function handleLogout() {
    logout()
    window.location.href = '/'
  }

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <Link className="hide-sm" to="/dashboard">
            Phòng của tôi
          </Link>
          <span className="text-muted">{user?.displayName}</span>
          <button className="btn btn-ghost" type="button" onClick={handleLogout}>
            Đăng xuất
          </button>
        </nav>
      </header>

      <main className="col pt-6 pb-12">
        {(connectionStatus === 'connecting' || state.status === null) && <p className="text-muted">Đang kết nối…</p>}

        {connectionStatus === 'disconnected' && (
          <div className="card grid justify-items-start gap-4">
            <p className="text-muted">Mất kết nối tới máy chủ.</p>
            <button className="btn btn-primary" type="button" onClick={reconnect}>
              Kết nối lại
            </button>
          </div>
        )}

        {connectionStatus !== 'disconnected' && state.status !== null && (
          <>
            {connectionStatus === 'reconnecting' && <p className="mb-2 text-xs text-faint">Đang kết nối lại…</p>}
            {lastError && (
              <p className="error mb-2 flex items-center justify-between gap-3" role="alert">
                {lastError.message}
                <button className="text-xs underline" type="button" onClick={clearError}>
                  Đóng
                </button>
              </p>
            )}
            {state.status === 'Lobby' && <LiveLobby state={state} />}
            {state.status === 'InProgress' && <LiveQuestion state={state} />}
            {state.status === 'Finished' && <LiveFinished state={state} />}
          </>
        )}
      </main>
    </>
  )
}
