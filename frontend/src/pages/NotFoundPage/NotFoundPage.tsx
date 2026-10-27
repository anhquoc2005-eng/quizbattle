import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col items-start justify-center gap-4 px-4">
      <h1 className="text-3xl font-bold">Không tìm thấy trang</h1>
      <p className="text-ink-muted">Đường dẫn này không tồn tại.</p>
      <Link to="/" className="text-brand-400 underline-offset-4 hover:underline">
        Về trang chủ
      </Link>
    </main>
  )
}
