import { useEffect } from 'react'
import { useNavigate, useParams } from 'react-router'
import { clearPlayerSession, loadPlayerSession } from '../../api/playerSession'
import { GameConnectionProvider } from '../../realtime/GameConnectionProvider'
import { shouldDiscardSession } from '../../realtime/sessionErrors'
import { useGameConnection } from '../../realtime/useGameConnection'
import { PlayFinished } from './PlayFinished'
import { PlayLobby } from './PlayLobby'
import { PlayQuestion } from './PlayQuestion'

export function PlayPage() {
  const params = useParams<{ code: string }>()
  const code = (params.code ?? '').toUpperCase()
  const navigate = useNavigate()
  const session = loadPlayerSession(code)

  useEffect(() => {
    if (!session) {
      navigate(`/join?code=${code}`, { replace: true })
    }
  }, [session, code, navigate])

  if (!session) {
    return null
  }

  return (
    <GameConnectionProvider code={code} sessionToken={session.sessionToken}>
      <PlayPageContent code={code} />
    </GameConnectionProvider>
  )
}

function PlayPageContent({ code }: { code: string }) {
  const { state, connectionStatus, lastError, clearError, reconnect } = useGameConnection()
  const navigate = useNavigate()

  useEffect(() => {
    if (!lastError) {
      return
    }

    if (shouldDiscardSession(lastError)) {
      clearPlayerSession(code)
      navigate(`/join?code=${code}`, { replace: true })
    }
  }, [lastError, code, navigate])

  if (connectionStatus === 'connecting' || state.status === null) {
    return (
      <main className="col flex min-h-dvh items-center justify-center">
        <p className="text-muted">Đang kết nối…</p>
      </main>
    )
  }

  if (connectionStatus === 'disconnected') {
    return (
      <main className="col flex min-h-dvh flex-col items-center justify-center gap-4">
        <p className="text-muted">Mất kết nối tới máy chủ.</p>
        <button className="btn btn-primary" type="button" onClick={reconnect}>
          Kết nối lại
        </button>
      </main>
    )
  }

  return (
    <main className="col grid gap-3.5 py-6 pb-12">
      {connectionStatus === 'reconnecting' && <p className="text-xs text-faint">Đang kết nối lại…</p>}
      {lastError && !shouldDiscardSession(lastError) && (
        <p className="error flex items-center justify-between gap-3" role="alert">
          {lastError.message}
          <button className="text-xs underline" type="button" onClick={clearError}>
            Đóng
          </button>
        </p>
      )}
      {state.status === 'Lobby' && <PlayLobby state={state} />}
      {state.status === 'InProgress' && <PlayQuestion state={state} />}
      {state.status === 'Finished' && <PlayFinished state={state} />}
    </main>
  )
}
