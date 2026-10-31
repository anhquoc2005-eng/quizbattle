import { Link } from 'react-router'
import { useAuth } from '../../hooks/useAuth'
import { BackendStatus } from './BackendStatus'

export function HomePage() {
  const { user, isLoading } = useAuth()

  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col justify-center gap-8 px-4 py-12">
      <header className="space-y-3">
        <h1 className="text-4xl font-bold sm:text-5xl">QuizBattle</h1>
        <p className="text-muted">
          Quiz real-time: nhập chủ đề, AI sinh câu hỏi, bạn bè vào phòng bằng mã và thi đấu trực tiếp.
        </p>
        {!isLoading && (
          <div className="flex gap-3.5">
            {user ? (
              <Link className="btn btn-primary" to="/dashboard">
                Vào trang của tôi
              </Link>
            ) : (
              <>
                <Link className="btn btn-primary" to="/login">
                  Đăng nhập
                </Link>
                <Link className="btn btn-ghost" to="/register">
                  Đăng ký
                </Link>
              </>
            )}
          </div>
        )}
      </header>

      <BackendStatus />
    </main>
  )
}
