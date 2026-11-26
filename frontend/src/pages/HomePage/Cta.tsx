import { Link } from 'react-router'
import { useAuth } from '../../hooks/useAuth'

/** Lời kêu gọi đảo màu: nền trắng chữ đen, tiêu đề lặp lại ý của hero. */
export function Cta() {
  const { user } = useAuth()
  return (
    <section aria-labelledby="cta-title" className="bg-ink text-ground">
      <div className="col flex lg:min-h-[min(90svh,760px)] flex-col justify-between gap-16 py-[clamp(48px,8vw,96px)]">
        <h2 id="cta-title" data-reveal className="text-[clamp(48px,8vw,120px)] font-semibold leading-[1] tracking-[-0.04em]">
          Mã đã sẵn sàng.
          <br />
          Còn bạn?
        </h2>
        <div data-reveal className="flex flex-col gap-6 sm:flex-row sm:items-end sm:justify-between">
          <p className="max-w-[34ch] text-lg">Nhập mã để vào trận, hoặc tạo phòng cho lớp và nhóm bạn.</p>
          <div className="flex flex-wrap gap-3">
            <Link className="btn h-14 bg-ground px-8 text-base text-ink hover:opacity-90" to="/join">
              Vào phòng
            </Link>
            <Link className="btn h-14 border-2 border-ground px-8 text-base text-ground" to={user ? '/dashboard' : '/register'}>
              {user ? 'Tạo phòng' : 'Đăng ký'}
            </Link>
          </div>
        </div>
      </div>
    </section>
  )
}
