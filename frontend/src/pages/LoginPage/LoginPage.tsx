import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { ApiError } from '../../api/apiError'
import { FormField } from '../../components/FormField'
import { HowItWorksCard } from '../../components/HowItWorksCard'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import { hasFormErrors, mapServerErrorsToFields, validateLoginForm, type LoginFormErrors } from '../../validation/authForm'

const LOGIN_FIELDS = ['email', 'password'] as const

export function LoginPage() {
  const { login, user, isLoading: authLoading } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<LoginFormErrors>({})
  const [formError, setFormError] = useState<string | undefined>()

  const mutation = useMutation({
    mutationFn: () => login(email, password),
    onSuccess: () => navigate(getRedirectPath(location.state), { replace: true }),
    onError: (error: unknown) => {
      setFormError(undefined)

      if (!(error instanceof ApiError)) {
        setFieldErrors({})
        setFormError('Không kết nối được máy chủ.')
        return
      }

      if (error.status === 400) {
        setFieldErrors(mapServerErrorsToFields(error.problem?.errors, LOGIN_FIELDS))
      } else if (error.status === 401) {
        setFieldErrors({ password: 'Email hoặc mật khẩu không đúng.' })
      } else if (error.status === 429) {
        setFieldErrors({})
        setFormError('Thử quá nhiều lần, đợi 1 phút.')
      } else {
        setFieldErrors({})
        setFormError(error.problem?.title ?? 'Có lỗi xảy ra.')
      }
    },
  })

  if (!authLoading && user) {
    return <Navigate to="/dashboard" replace />
  }

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    const errors = validateLoginForm({ email, password })
    setFieldErrors(errors)
    setFormError(undefined)
    if (!hasFormErrors(errors)) {
      mutation.mutate()
    }
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
            <FormField
              id="email"
              label="Email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={setEmail}
              error={fieldErrors.email}
            />
            <FormField
              id="password"
              label="Mật khẩu"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={setPassword}
              error={fieldErrors.password}
            />
            {formError && (
              <p className="error" role="alert">
                {formError}
              </p>
            )}
            <button className="btn btn-primary mt-2 justify-self-stretch" type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Đang xử lý…' : 'Đăng nhập'}
            </button>
          </form>
        </section>
        <HowItWorksCard />
      </main>
    </>
  )
}

interface LocationState {
  from?: { pathname: string }
}

function isLocationState(value: unknown): value is LocationState {
  return typeof value === 'object' && value !== null
}

function getRedirectPath(state: unknown): string {
  if (isLocationState(state) && typeof state.from?.pathname === 'string') {
    return state.from.pathname
  }
  return '/dashboard'
}
