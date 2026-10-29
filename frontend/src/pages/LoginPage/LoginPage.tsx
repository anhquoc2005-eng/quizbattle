import { useState } from 'react'
import { Link } from 'react-router'
import { HowItWorksCard } from '../../components/HowItWorksCard'
import { LogoMark } from '../../components/LogoMark'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    // TODO: nối POST /api/auth/login khi làm tính năng xác thực ở frontend.
  }

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <span className="hide-sm text-muted">Chưa có tài khoản?</span>
          <Link to="/register">Đăng ký</Link>
        </nav>
      </header>
      <main className="col grid grid-cols-1 content-center items-start gap-3.5 min-[861px]:grid-cols-2 min-h-[calc(100dvh-48px)] py-8 pb-12">
        <section className="card" aria-labelledby="login-title">
          <form className="grid content-start gap-4" onSubmit={handleSubmit} noValidate>
            <div>
              <h1 id="login-title">Đăng nhập chủ phòng</h1>
              <p className="mt-1 text-muted">Người chơi không cần tài khoản, chỉ cần mã phòng.</p>
            </div>
            <div className="field">
              <label htmlFor="email">Email</label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            </div>
            <div className="field">
              <label htmlFor="password">Mật khẩu</label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            </div>
            <button className="btn btn-primary mt-2 justify-self-stretch" type="submit">
              Đăng nhập
            </button>
          </form>
        </section>
        <HowItWorksCard />
      </main>
    </>
  )
}
