import { Link, useParams } from 'react-router'
import { ApiError, describeApiError } from '../../api/apiError'
import { ErrorRetry } from '../../components/ErrorRetry'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import { useRoom } from '../../hooks/useRoom'
import { GenerateQuestionsPanel } from './GenerateQuestionsPanel'
import { QuestionList } from './QuestionList'
import { RoomHeader } from './RoomHeader'

export function RoomPage() {
  const { user, logout } = useAuth()
  const params = useParams<{ code: string }>()
  // Chuẩn hóa hoa 1 lần ở đây, truyền xuống mọi con/mutation để /rooms/k7q2mx và /rooms/K7Q2MX dùng chung cache.
  const code = (params.code ?? '').toUpperCase()
  const roomQuery = useRoom(code)

  function handleLogout() {
    logout()
    window.location.href = '/'
  }

  if (roomQuery.isPending) {
    return (
      <main className="col flex min-h-dvh items-center justify-center">
        <p className="text-muted">Đang tải…</p>
      </main>
    )
  }

  if (roomQuery.isError) {
    if (roomQuery.error instanceof ApiError && roomQuery.error.status === 404) {
      return <RoomNotFound />
    }

    return (
      <main className="col flex min-h-dvh items-center justify-center">
        <ErrorRetry message={describeApiError(roomQuery.error)} onRetry={() => roomQuery.refetch()} />
      </main>
    )
  }

  const room = roomQuery.data

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
        <RoomHeader room={room} />
        {room.status === 'Draft' && <GenerateQuestionsPanel code={code} />}
        <QuestionList code={code} room={room} />
      </main>
    </>
  )
}

function RoomNotFound() {
  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col items-start justify-center gap-4 px-4">
      <h1 className="text-3xl font-bold">Không tìm thấy phòng</h1>
      <p className="text-muted">Mã phòng không đúng hoặc phòng không thuộc về bạn.</p>
      <Link to="/dashboard" className="underline-offset-4 hover:underline">
        Về Phòng của tôi
      </Link>
    </main>
  )
}
