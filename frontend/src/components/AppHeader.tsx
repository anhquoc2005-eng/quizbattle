import { Link, useLocation } from 'react-router'
import { useAuth } from '../hooks/useAuth'
import { LogoMark } from './LogoMark'

export type AppHeaderPage = 'dashboard' | 'practice' | 'shop' | 'ranking' | 'profile'

function pageProps(page: AppHeaderPage, current?: AppHeaderPage) {
  return page === current ? { 'aria-current': 'page' as const } : {}
}

export function AppHeader({ current }: { current?: AppHeaderPage }) {
  const { user, isLoading, logout } = useAuth()
  const location = useLocation()

  function handleLogout() {
    // Tải lại trang thật (không điều hướng SPA): tránh đua với ProtectedRoute - nó cũng đổi sang
    // /login ngay khi user = null nếu còn điều hướng bằng navigate() trong lúc đang ở trang cần đăng nhập.
    // Dọn sạch state React (cache TanStack của tài khoản cũ) cũng là điều nên làm khi đăng xuất.
    logout()
    window.location.href = '/'
  }

  return (
    <header className="col bar">
      <Link className="mark" to="/">
        <LogoMark />
        QuizBattle
      </Link>
      <nav>
        {user && (
          <>
            <Link className="hide-sm" to="/dashboard" {...pageProps('dashboard', current)}>
              Phòng của tôi
            </Link>
            <Link className="hide-sm" to="/practice" {...pageProps('practice', current)}>
              Ôn tập
            </Link>
          </>
        )}
        <Link className={user ? 'hide-sm' : undefined} to="/shop" {...pageProps('shop', current)}>
          Cửa hàng
        </Link>
        <Link className={user ? 'hide-sm' : undefined} to="/ranking" {...pageProps('ranking', current)}>
          Xếp hạng
        </Link>
        {!isLoading &&
          (user ? (
            <>
              <Link to="/profile" {...pageProps('profile', current)}>
                {user.displayName}
              </Link>
              <button className="btn btn-ghost" type="button" onClick={handleLogout}>
                Đăng xuất
              </button>
            </>
          ) : (
            <Link to="/login" state={{ from: location }}>
              Đăng nhập
            </Link>
          ))}
      </nav>
    </header>
  )
}
