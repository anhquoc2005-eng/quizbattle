import { useState } from 'react'
import { Link } from 'react-router'
import { describeApiError } from '../../api/apiError'
import { ErrorRetry } from '../../components/ErrorRetry'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import { useRooms } from '../../hooks/useRooms'
import { getSetupProgress, type StepStatus } from './setupProgress'
import { CreateRoomForm } from './CreateRoomForm'
import { RoomList } from './RoomList'

const TOTAL_STEPS = 3

export function DashboardPage() {
  const { user, logout } = useAuth()
  const roomsQuery = useRooms()
  const [showCreateForm, setShowCreateForm] = useState(false)

  function handleLogout() {
    // Tải lại trang thật (không điều hướng SPA): tránh đua với ProtectedRoute - nó cũng đổi sang
    // /login ngay khi user = null nếu còn điều hướng bằng navigate() trong lúc đang ở /dashboard.
    // Dọn sạch state React cũng là điều nên làm khi đăng xuất.
    logout()
    window.location.href = '/'
  }

  const rooms = roomsQuery.data ?? []
  const hasAnyRoom = rooms.length > 0
  const openRooms = rooms.filter((room) => room.status !== 'Finished')
  const finishedRooms = rooms.filter((room) => room.status === 'Finished')
  const { doneSteps, createRoomStatus } = getSetupProgress(roomsQuery.status, hasAnyRoom)

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <Link className="hide-sm" to="/dashboard" aria-current="page">
            Phòng của tôi
          </Link>
          <Link className="hide-sm" to="/ranking">
            Xếp hạng
          </Link>
          <Link to="/profile">{user?.displayName}</Link>
          <button className="btn btn-ghost" type="button" onClick={handleLogout}>
            Đăng xuất
          </button>
        </nav>
      </header>

      <main className="col">
        <div className="flex flex-wrap items-center justify-between gap-4 pt-6 pb-4">
          <div>
            <h1>Phòng của tôi</h1>
            <p className="text-muted">
              {roomsQuery.isPending ? 'Đang tải…' : roomsQuery.isError ? 'Không tải được danh sách phòng.' : hasAnyRoom ? `Bạn có ${rooms.length} phòng.` : 'Bạn chưa tạo phòng nào.'}
            </p>
          </div>
          <button className="btn btn-primary" type="button" onClick={() => setShowCreateForm((value) => !value)}>
            Tạo phòng
          </button>
        </div>

        <div className="grid grid-cols-1 gap-3.5 pb-12 min-[861px]:grid-cols-[2fr_1fr]">
          {showCreateForm && <CreateRoomForm onCancel={() => setShowCreateForm(false)} />}

          <section className="card col-span-full grid grid-cols-[auto_1fr] items-center gap-x-6 gap-y-2" aria-labelledby="setup-title">
            <h2 id="setup-title">Bắt đầu với QuizBattle</h2>
            <span className="justify-self-end text-xs text-faint">{roomsQuery.isPending ? 'Đang tải…' : roomsQuery.isError ? 'Không tải được' : `${doneSteps} trên ${TOTAL_STEPS} bước`}</span>
            <div
              className="col-span-full h-1.5 overflow-hidden rounded-full bg-raised"
              role="progressbar"
              aria-valuenow={doneSteps}
              aria-valuemin={0}
              aria-valuemax={TOTAL_STEPS}
              aria-label="Tiến độ bắt đầu"
            >
              <span className="block h-full bg-accent" style={{ width: `${(doneSteps / TOTAL_STEPS) * 100}%` }} />
            </div>
            <div className="col-span-full mt-2 grid grid-cols-1 gap-3.5 min-[861px]:grid-cols-3">
              <StepCard title="Tạo tài khoản" status="done" />
              <StepCard title="Tạo phòng đầu tiên" description="Nhập chủ đề, AI soạn câu hỏi" status={createRoomStatus} />
              <StepCard title="Mời người chơi" description="Gửi mã phòng cho bạn bè" status="todo" />
            </div>
          </section>

          <section className="card" aria-labelledby="open-rooms-title">
            <h2 id="open-rooms-title">Phòng đang mở</h2>
            {roomsQuery.isPending ? (
              <p className="mt-4 text-muted">Đang tải…</p>
            ) : roomsQuery.isError ? (
              <ErrorRetry message={describeApiError(roomsQuery.error)} onRetry={() => roomsQuery.refetch()} />
            ) : (
              <RoomList rooms={openRooms} emptyMessage="Khi bạn tạo phòng, mã phòng sẽ hiện ở đây để gửi cho người chơi." />
            )}
          </section>

          <section className="card" aria-labelledby="history-title">
            <h2 id="history-title">Lịch sử trận</h2>
            {roomsQuery.isPending ? (
              <p className="mt-4 text-muted">Đang tải…</p>
            ) : roomsQuery.isError ? (
              <ErrorRetry message={describeApiError(roomsQuery.error)} onRetry={() => roomsQuery.refetch()} />
            ) : (
              <RoomList rooms={finishedRooms} emptyMessage="Chưa có trận nào. Trận đã chơi sẽ lưu ở đây kèm bảng xếp hạng cuối cùng." />
            )}
          </section>
        </div>
      </main>
    </>
  )
}

function StepCard({ title, description, status }: { title: string; description?: string; status: StepStatus }) {
  return (
    <div className="grid gap-1 rounded-[14px] bg-raised px-4 py-3.5">
      <b className="font-semibold">{title}</b>
      {status === 'done' ? (
        <span className="text-xs text-positive">Đã xong</span>
      ) : status === 'error' ? (
        <span className="text-muted">Không tải được</span>
      ) : status === 'loading' ? (
        <span className="text-muted">Đang tải…</span>
      ) : (
        <span className="text-muted">{description}</span>
      )}
    </div>
  )
}
