import { useState } from 'react'
import { Link, useLocation } from 'react-router'
import { useAuth } from '../hooks/useAuth'
import { LogoMark } from './LogoMark'

export type AppHeaderPage = 'dashboard' | 'practice' | 'shop' | 'ranking' | 'profile'

function pageProps(page: AppHeaderPage, current?: AppHeaderPage) {
  return page === current ? { 'aria-current': 'page' as const } : {}
}

const MENU_LINKS: { page: AppHeaderPage; to: string; label: string }[] = [
  { page: 'dashboard', to: '/dashboard', label: 'Phòng của tôi' },
  { page: 'practice', to: '/practice', label: 'Ôn tập' },
  { page: 'shop', to: '/shop', label: 'Cửa hàng' },
  { page: 'ranking', to: '/ranking', label: 'Xếp hạng' },
]

export function AppHeader({ current }: { current?: AppHeaderPage }) {
  const { user, isLoading, logout } = useAuth()
  const location = useLocation()
  // <details> không tự đóng khi React Router đổi trang, nên giữ trạng thái mở ở đây và đóng trong onClick của link.
  const [menuOpen, setMenuOpen] = useState(false)

  function handleLogout() {
    // Tải lại trang thật (không điều hướng SPA): tránh đua với ProtectedRoute - nó cũng đổi sang
    // /login ngay khi user = null nếu còn điều hướng bằng navigate() trong lúc đang ở trang cần đăng nhập.
    // Dọn sạch state React (cache TanStack của tài khoản cũ) cũng là điều nên làm khi đăng xuất.
    logout()
    window.location.href = '/'
  }

  return (
    <header className={user ? 'col bar signed-in' : 'col bar'}>
      <Link className="mark" to="/" aria-label="QuizBattle">
        <LogoMark />
        <span className="mark-text">QuizBattle</span>
      </Link>
      <nav>
        {user && (
          <details className="show-sm" open={menuOpen} onToggle={(event) => setMenuOpen(event.currentTarget.open)}>
            <summary className="btn btn-ghost">Menu</summary>
            <div className="menu-panel">
              {MENU_LINKS.map(({ page, to, label }) => (
                <Link key={page} to={to} onClick={() => setMenuOpen(false)} {...pageProps(page, current)}>
                  {label}
                </Link>
              ))}
            </div>
          </details>
        )}
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
              <Link className="user-name" to="/profile" title={user.displayName} {...pageProps('profile', current)}>
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
