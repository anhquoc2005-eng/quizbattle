import { BackendStatus } from './BackendStatus'

export function HomePage() {
  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col justify-center gap-8 px-4 py-12">
      <header className="space-y-3">
        <h1 className="text-4xl font-bold sm:text-5xl">QuizBattle</h1>
        <p className="text-muted">
          Quiz real-time: nhập chủ đề, AI sinh câu hỏi, bạn bè vào phòng bằng mã và thi đấu trực tiếp.
        </p>
      </header>

      <BackendStatus />
    </main>
  )
}
