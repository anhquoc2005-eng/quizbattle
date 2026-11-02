import { Link, useParams } from 'react-router'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import { GameConnectionProvider } from '../../realtime/GameConnectionProvider'
import { getScreen } from '../../realtime/gameScreen'
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
  const screen = getScreen(connectionStatus, state, lastError)

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
        {screen === 'connecting' && <p className="text-muted">Đang kết nối…</p>}

        {screen === 'joinError' && lastError?.code === 'unauthorized' && (
          <div className="card grid justify-items-start gap-4">
            <p className="text-negative" role="alert">
              Phiên đăng nhập đã hết hạn.
            </p>
            {/* logout() đặt user = null, ProtectedRoute tự chuyển về /login và quay lại đây sau khi đăng nhập. */}
            <button className="btn btn-primary" type="button" onClick={logout}>
              Đăng nhập lại
            </button>
          </div>
        )}

        {screen === 'joinError' && lastError?.code !== 'unauthorized' && (
          <div className="card grid justify-items-start gap-4">
            <p className="text-negative" role="alert">
              {lastError?.message}
            </p>
            <button className="btn btn-primary" type="button" onClick={reconnect}>
              Thử lại
            </button>
          </div>
        )}

        {screen === 'disconnected' && (
          <div className="card grid justify-items-start gap-4">
            <p className="text-muted">Mất kết nối tới máy chủ.</p>
            <button className="btn btn-primary" type="button" onClick={reconnect}>
              Kết nối lại
            </button>
          </div>
        )}

        {screen === 'game' && (
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
