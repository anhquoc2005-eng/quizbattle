import { Link } from 'react-router'
import { useAuth } from '../../hooks/useAuth'

/** Chân trang đảo màu: tên QuizBattle cỡ lớn, bị cắt ở đáy và mép phải bởi overflow-hidden. */
export function SiteFooter() {
  const { user } = useAuth()
  return (
    <footer className="overflow-hidden bg-ink text-ground">
      <div className="col flex flex-wrap items-center justify-between gap-4 pb-6 text-base">
        <span>QuizBattle: quiz trực tiếp, thi đấu bằng điện thoại.</span>
        <nav aria-label="Liên kết chân trang" className="flex flex-wrap gap-6">
          <Link className="inline-flex min-h-11 items-center" to="/shop">
            Cửa hàng
          </Link>
          <Link className="inline-flex min-h-11 items-center" to="/ranking">
            Xếp hạng
          </Link>
          {!user && (
            <Link className="inline-flex min-h-11 items-center" to="/login">
              Đăng nhập
            </Link>
          )}
        </nav>
      </div>
      <p
        aria-hidden="true"
        className="col translate-y-[26%] whitespace-nowrap text-[clamp(56px,17vw,240px)] font-semibold leading-[0.9] tracking-[-0.05em]"
      >
        QuizBattle
      </p>
    </footer>
  )
}
