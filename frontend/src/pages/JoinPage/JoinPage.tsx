import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router'
import { ApiError, describeApiError, describeJoinAuthError } from '../../api/apiError'
import { createPlayer } from '../../api/game'
import { savePlayerSession } from '../../api/playerSession'
import { isSessionExpiredFlagSet, loadAuth, markSessionExpired } from '../../api/tokenStorage'
import { FormField } from '../../components/FormField'
import { LogoMark } from '../../components/LogoMark'
import { SessionExpiredDialog } from '../../components/SessionExpiredDialog'
import { useAuth } from '../../hooks/useAuth'
import { hasFormErrors, mapServerErrorsToFields } from '../../validation/authForm'
import { validateJoinForm, type JoinFormErrors } from '../../validation/joinForm'

const JOIN_FIELDS = ['code', 'displayName'] as const

/** Thông báo do trang trước gửi kèm khi chuyển về /join (ví dụ bị thiết bị khác thay thế). */
function readNotice(state: unknown): string | null {
  const notice = typeof state === 'object' && state !== null ? (state as { notice?: unknown }).notice : undefined
  return typeof notice === 'string' && notice !== '' ? notice : null
}

/** Màn công khai để người chơi vào phòng bằng mã - không cần tài khoản. */
export function JoinPage() {
  const navigate = useNavigate()
  const notice = readNotice(useLocation().state)
  const { sessionExpired, dismissSessionExpired } = useAuth()
  const [searchParams] = useSearchParams()
  const [code, setCode] = useState((searchParams.get('code') ?? '').toUpperCase())
  const [displayName, setDisplayName] = useState('')
  const [fieldErrors, setFieldErrors] = useState<JoinFormErrors>({})
  const [formError, setFormError] = useState<string>()

  const mutation = useMutation({
    mutationFn: () => createPlayer(code.trim().toUpperCase(), { displayName: displayName.trim() }),
    onSuccess: (result) => {
      savePlayerSession(result.roomCode, { playerId: result.playerId, sessionToken: result.sessionToken })
      navigate(`/play/${result.roomCode}`)
    },
    onError: (error: unknown) => {
      setFormError(undefined)
      if (error instanceof ApiError && error.status === 401 && describeJoinAuthError(error.problem) === 'expired') {
        // Token bị server từ chối: request() đã xóa nó. Báo bằng hộp thoại, không bao giờ tự gửi lại như khách.
        setFieldErrors({})
        markSessionExpired()
      } else if (error instanceof ApiError && error.status === 400) {
        setFieldErrors(mapServerErrorsToFields(error.problem?.errors, JOIN_FIELDS))
      } else if (error instanceof ApiError && error.status === 404) {
        setFieldErrors({ code: 'Không tìm thấy phòng.' })
      } else {
        setFieldErrors({})
        setFormError(describeApiError(error))
      }
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    // Token có thể hết hạn khi người dùng đang ở trang: loadAuth() xóa và đặt cờ, rồi không gửi (nếu gửi, request() sẽ
    // không kèm token và server coi là khách).
    loadAuth()
    if (isSessionExpiredFlagSet()) {
      return
    }

    const errors = validateJoinForm({ code, displayName })
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
      </header>
      <main className="col flex min-h-[calc(100dvh-48px)] items-center justify-center py-8">
        <section className="card w-full max-w-md" aria-labelledby="join-title">
          <form className="grid gap-4" onSubmit={handleSubmit} noValidate>
            <div>
              <h1 id="join-title">Vào phòng</h1>
              <p className="mt-1 text-muted">Nhập mã phòng chủ phòng đã gửi cho bạn.</p>
              {notice && (
                <p className="mt-2" role="status">
                  {notice}
                </p>
              )}
            </div>
            <FormField
              id="code"
              label="Mã phòng"
              value={code}
              onChange={(value) => setCode(value.toUpperCase())}
              error={fieldErrors.code}
              hint="6 ký tự."
            />
            <FormField id="displayName" label="Tên hiển thị" value={displayName} onChange={setDisplayName} error={fieldErrors.displayName} />
            {formError && (
              <p className="error" role="alert">
                {formError}
              </p>
            )}
            <button className="btn btn-primary justify-self-stretch" type="submit" disabled={mutation.isPending || sessionExpired}>
              {mutation.isPending ? 'Đang vào…' : 'Vào phòng'}
            </button>
          </form>
        </section>
      </main>
      {sessionExpired && (
        <SessionExpiredDialog
          onLoginAgain={() =>
            navigate('/login', { state: { from: { pathname: '/join', search: code.trim() ? `?code=${code.trim().toUpperCase()}` : '' } } })
          }
          onContinueAsGuest={dismissSessionExpired}
        />
      )}
    </>
  )
}
