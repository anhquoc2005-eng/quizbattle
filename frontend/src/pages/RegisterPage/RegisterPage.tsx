import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { ApiError } from '../../api/apiError'
import { FormField } from '../../components/FormField'
import { HowItWorksCard } from '../../components/HowItWorksCard'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import {
  hasFormErrors,
  mapServerErrorsToFields,
  validateRegisterForm,
  type RegisterFormErrors,
} from '../../validation/authForm'

const REGISTER_FIELDS = ['displayName', 'email', 'password'] as const

export function RegisterPage() {
  const { register, user, isLoading: authLoading } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [fieldErrors, setFieldErrors] = useState<RegisterFormErrors>({})
  const [formError, setFormError] = useState<string | undefined>()

  const mutation = useMutation({
    mutationFn: () => register(email, password, displayName),
    onSuccess: () => navigate('/dashboard', { replace: true }),
    onError: (error: unknown) => {
      setFormError(undefined)

      if (!(error instanceof ApiError)) {
        setFieldErrors({})
        setFormError('Không kết nối được máy chủ.')
        return
      }

      if (error.status === 400) {
        setFieldErrors(mapServerErrorsToFields(error.problem?.errors, REGISTER_FIELDS))
      } else if (error.status === 409) {
        setFieldErrors({ email: 'Email đã được sử dụng.' })
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
    const errors = validateRegisterForm({ displayName, email, password })
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
            <FormField
              id="displayName"
              label="Tên hiển thị"
              autoComplete="name"
              value={displayName}
              onChange={setDisplayName}
              error={fieldErrors.displayName}
            />
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
              autoComplete="new-password"
              value={password}
              onChange={setPassword}
              error={fieldErrors.password}
              hint="8–128 ký tự."
            />
            {formError && (
              <p className="error" role="alert">
                {formError}
              </p>
            )}
            <button className="btn btn-primary mt-2 justify-self-stretch" type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Đang xử lý…' : 'Đăng ký'}
            </button>
          </form>
        </section>
        <HowItWorksCard />
      </main>
    </>
  )
}
