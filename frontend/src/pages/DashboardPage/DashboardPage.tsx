import { Link } from 'react-router'
import { LogoMark } from '../../components/LogoMark'
import { RoomCodeBoxes } from '../../components/RoomCodeBoxes'

// TODO: lấy từ phiên đăng nhập thật khi nối API xác thực ở frontend.
const CURRENT_USER_NAME = 'Quốc'

export function DashboardPage() {
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
          <span className="text-muted">{CURRENT_USER_NAME}</span>
          <button className="btn btn-ghost" type="button">
            Đăng xuất
          </button>
        </nav>
      </header>

      <main className="col">
        <div className="flex flex-wrap items-center justify-between gap-4 pt-6 pb-4">
          <div>
            <h1>Phòng của tôi</h1>
            <p className="text-muted">Bạn chưa tạo phòng nào.</p>
          </div>
          <div className="grid justify-items-end gap-1 max-[860px]:justify-items-start">
            <button className="btn btn-primary" type="button" disabled aria-describedby="create-room-soon">
              Tạo phòng
            </button>
            <span id="create-room-soon" className="text-xs text-faint">
              Tính năng tạo phòng đang được làm
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3.5 pb-12 min-[861px]:grid-cols-[2fr_1fr]">
          <section className="card col-span-full grid grid-cols-[auto_1fr] items-center gap-x-6 gap-y-2" aria-labelledby="setup-title">
            <h2 id="setup-title">Bắt đầu với QuizBattle</h2>
            <span className="justify-self-end text-xs text-faint">1 trên 3 bước</span>
            <div
              className="col-span-full h-1.5 overflow-hidden rounded-full bg-raised"
              role="progressbar"
              aria-valuenow={1}
              aria-valuemin={0}
              aria-valuemax={3}
              aria-label="Tiến độ bắt đầu"
            >
              <span className="block h-full bg-accent" style={{ width: '33.3%' }} />
            </div>
            <div className="col-span-full mt-2 grid grid-cols-1 gap-3.5 min-[861px]:grid-cols-3">
              <StepCard title="Tạo tài khoản" done />
              <StepCard title="Tạo phòng đầu tiên" description="Nhập chủ đề, AI soạn câu hỏi" />
              <StepCard title="Mời người chơi" description="Gửi mã phòng cho bạn bè" />
            </div>
          </section>

          <section className="card" aria-labelledby="open-rooms-title">
            <h2 id="open-rooms-title">Phòng đang mở</h2>
            <div className="mt-4 grid justify-items-start gap-4 rounded-[14px] border border-dashed border-control p-5">
              <RoomCodeBoxes chars={['?', '?', '?', '?', '?', '?']} variant="slot" />
              <p className="text-muted">Khi bạn tạo phòng, mã phòng sẽ hiện ở đây để gửi cho người chơi.</p>
            </div>
          </section>

          <section className="card" aria-labelledby="history-title">
            <h2 id="history-title">Lịch sử trận</h2>
            <div className="mt-4 grid gap-2">
              <div className="grid grid-cols-[1fr_auto_auto] gap-4 border-b border-rule px-1 pb-2 text-xs text-faint">
                <span>Chủ đề</span>
                <span>Người chơi</span>
                <span>Ngày</span>
              </div>
              <div className="grid justify-items-start gap-4 rounded-[14px] border border-dashed border-control p-5">
                <p className="text-muted">Chưa có trận nào. Trận đã chơi sẽ lưu ở đây kèm bảng xếp hạng cuối cùng.</p>
              </div>
            </div>
          </section>
        </div>
      </main>
    </>
  )
}

function StepCard({ title, description, done }: { title: string; description?: string; done?: boolean }) {
  return (
    <div className="grid gap-1 rounded-[14px] bg-raised px-4 py-3.5">
      <b className="font-semibold">{title}</b>
      {done ? <span className="text-xs text-positive">Đã xong</span> : <span className="text-muted">{description}</span>}
    </div>
  )
}
