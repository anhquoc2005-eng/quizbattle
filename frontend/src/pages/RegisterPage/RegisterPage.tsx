import { useState } from 'react'
import { Link } from 'react-router'
import { HowItWorksCard } from '../../components/HowItWorksCard'
import { LogoMark } from '../../components/LogoMark'

export function RegisterPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    // TODO: nối POST /api/auth/register khi làm tính năng xác thực ở frontend.
  }

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <span className="hide-sm text-muted">Đã có tài khoản?</span>
          <Link to="/login">Đăng nhập</Link>
        </nav>
      </header>
      <main className="col grid grid-cols-1 content-center items-start gap-3.5 min-[861px]:grid-cols-2 min-h-[calc(100dvh-48px)] py-8 pb-12">
        <section className="card" aria-labelledby="register-title">
          <form className="grid content-start gap-4" onSubmit={handleSubmit} noValidate>
            <div>
              <h1 id="register-title">Đăng ký chủ phòng</h1>
              <p className="mt-1 text-muted">Tạo tài khoản để tạo phòng và quản lý lịch sử trận.</p>
            </div>
            <div className="field">
              <label htmlFor="displayName">Tên hiển thị</label>
              <input
                id="displayName"
                type="text"
                autoComplete="name"
                value={displayName}
                onChange={(event) => setDisplayName(event.target.value)}
              />
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
                autoComplete="new-password"
                aria-describedby="password-hint"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
              <p id="password-hint" className="text-xs text-faint">
                8–128 ký tự.
              </p>
            </div>
            <button className="btn btn-primary mt-2 justify-self-stretch" type="submit">
              Đăng ký
            </button>
          </form>
        </section>
        <HowItWorksCard />
      </main>
    </>
  )
}
