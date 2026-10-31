import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from '../hooks/useAuth'

/** Bọc quanh route cần đăng nhập. Chưa đăng nhập thì chuyển về /login, giữ lại trang định vào. */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <main className="col flex min-h-dvh items-center justify-center">
        <p className="text-muted">Đang tải…</p>
      </main>
    )
  }

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return children
}
