import { Link } from 'react-router'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'

const GITHUB_URL = 'https://github.com/anhquoc2005-eng/quizbattle'
// Nền footer là trắng: text-muted (#b4b4b4) chỉ ~1.9:1, nên dùng chữ đen mờ 70% (~8.4:1).
const LINK = 'inline-flex items-center text-sm text-ground/70 hover:text-ground max-sm:min-h-[44px]'

function Column({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <nav aria-label={title} className="flex flex-col gap-1 md:gap-2">
      <h2 className="text-sm font-semibold">{title}</h2>
      {children}
    </nav>
  )
}

/** Chân trang gọn, nền đảo màu nối liền khối Cta phía trên. */
export function SiteFooter() {
  const { user } = useAuth()
  return (
    <footer className="border-t border-ground/20 bg-ink text-ground">
      <div className="col grid grid-cols-2 gap-x-6 gap-y-8 py-10 md:grid-cols-4">
        <div className="col-span-2 flex flex-col gap-3 md:col-span-1">
          <Link className="mark text-lg max-sm:min-h-[44px]" to="/" aria-label="QuizBattle">
            <LogoMark />
            <span>QuizBattle</span>
          </Link>
          <p className="max-w-[28ch] text-sm text-ground/70">Quiz trực tiếp, thi đấu bằng điện thoại.</p>
        </div>
        <Column title="Chơi">
          <Link className={LINK} to="/join">Vào phòng</Link>
          <Link className={LINK} to={user ? '/dashboard' : '/register'}>Tạo phòng</Link>
        </Column>
        <Column title="Khám phá">
          <Link className={LINK} to="/shop">Cửa hàng</Link>
          <Link className={LINK} to="/ranking">Xếp hạng</Link>
        </Column>
        <Column title="Tài khoản">
          {user ? (
            <Link className={LINK} to="/dashboard">Bảng điều khiển</Link>
          ) : (
            <>
              <Link className={LINK} to="/login">Đăng nhập</Link>
              <Link className={LINK} to="/register">Đăng ký</Link>
            </>
          )}
        </Column>
      </div>
      <div className="border-t border-ground/20">
        <div className="col flex flex-wrap items-center justify-between gap-x-6 text-sm text-ground/70">
          <span className="py-3">© 2026 QuizBattle</span>
          <a className={LINK} href={GITHUB_URL} target="_blank" rel="noopener noreferrer">GitHub</a>
        </div>
      </div>
    </footer>
  )
}
