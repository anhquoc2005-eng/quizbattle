// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import { login as loginRequest, getMe } from '../../api/auth'
import { createPlayer } from '../../api/game'
import { clearSessionExpiredFlag, isSessionExpiredFlagSet, saveAuth } from '../../api/tokenStorage'
import { AuthProvider } from '../../components/AuthProvider'
import { useAuth } from '../../hooks/useAuth'
import type { AuthResponse } from '../../types/auth'
import { JoinPage } from './JoinPage'

// Chỉ giả lập tầng gọi mạng; AuthProvider, tokenStorage và localStorage (jsdom) là thật.
vi.mock('../../api/game', () => ({ createPlayer: vi.fn() }))
vi.mock('../../api/auth', () => ({ getMe: vi.fn(), login: vi.fn(), register: vi.fn() }))

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }

function auth(expiresInMs: number): AuthResponse {
  return { token: 'jwt', expiresAt: new Date(Date.now() + expiresInMs).toISOString(), user: USER }
}

function LoginProbe() {
  const location = useLocation()
  const { login } = useAuth()
  const from = (location.state as { from?: { pathname: string; search?: string } } | null)?.from
  return (
    <div>
      <p>login-from:{from ? `${from.pathname}${from.search ?? ''}` : 'none'}</p>
      <button type="button" onClick={() => void login('a@b.c', 'pw')}>
        probe-login
      </button>
    </div>
  )
}

function LogoutProbe() {
  const { logout } = useAuth()
  return (
    <button type="button" onClick={logout}>
      probe-logout
    </button>
  )
}

function renderJoin(state?: unknown) {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { mutations: { retry: false } } })}>
      <MemoryRouter initialEntries={[{ pathname: '/join', search: '?code=ABCDEF', state }]}>
        <AuthProvider>
          <Routes>
            <Route path="/join" element={<JoinPage />} />
            <Route path="/login" element={<LoginProbe />} />
            <Route path="/play/:code" element={<p>trang-choi</p>} />
          </Routes>
          <LogoutProbe />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function fillName(name: string) {
  fireEvent.change(screen.getByLabelText('Tên hiển thị'), { target: { value: name } })
}

function submit() {
  fireEvent.submit(screen.getByRole('button', { name: /Vào phòng|Đang vào/ }).closest('form')!)
}

beforeEach(() => {
  localStorage.clear()
  clearSessionExpiredFlag() // cờ còn bản trong bộ nhớ của module, localStorage.clear() không xóa được
  vi.mocked(createPlayer).mockReset()
  vi.mocked(getMe).mockReset().mockResolvedValue(USER)
  vi.mocked(loginRequest).mockReset().mockResolvedValue(auth(60_000))
})

afterEach(cleanup)

describe('JoinPage: phiên đăng nhập hết hạn', () => {
  it('JoinPage_FlagSetFromLoadAuth_ShowsExpiredDialogOnMount', async () => {
    saveAuth(auth(-1000)) // token đã hết hạn lúc mở trang: loadAuth() xóa và đặt cờ

    renderJoin()

    expect(await screen.findByRole('dialog', { name: 'Phiên đăng nhập đã hết hạn' })).toBeTruthy()
  })

  it('JoinPage_Server401TokenExpired_ShowsDialogAndKeepsItAfterUserBecomesNull', async () => {
    saveAuth(auth(60_000))
    vi.mocked(createPlayer).mockRejectedValue(new ApiError(401, { title: 'Phiên đăng nhập đã hết hạn.', code: 'token_expired' }))
    renderJoin()
    await waitFor(() => expect(getMe).toHaveBeenCalled())
    fillName('Alice')

    submit()
    expect(await screen.findByRole('dialog', { name: 'Phiên đăng nhập đã hết hạn' })).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull() // hộp thoại là thông báo, không hiện lỗi form

    fireEvent.click(screen.getByRole('button', { name: 'probe-logout' })) // user -> null
    expect(screen.getByRole('dialog', { name: 'Phiên đăng nhập đã hết hạn' })).toBeTruthy()
  })

  it('JoinPage_ExpiredDialogOpen_SubmitIsDisabledAndNothingIsSent', async () => {
    saveAuth(auth(-1000))
    renderJoin()
    await screen.findByRole('dialog')
    fillName('Alice')

    expect((screen.getByRole('button', { name: 'Vào phòng' }) as HTMLButtonElement).disabled).toBe(true)
    submit()

    expect(createPlayer).not.toHaveBeenCalled()
  })

  it('JoinPage_ChooseGuest_ClearsFlagAndClosesDialog', async () => {
    saveAuth(auth(-1000))
    vi.mocked(createPlayer).mockResolvedValue({ playerId: 'p1', sessionToken: 's', roomCode: 'ABCDEF' })
    renderJoin()
    await screen.findByRole('dialog')

    fireEvent.click(screen.getByRole('button', { name: 'Vào làm khách' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    expect(isSessionExpiredFlagSet()).toBe(false)
    fillName('Khách')
    submit()
    await waitFor(() => expect(createPlayer).toHaveBeenCalledWith('ABCDEF', { displayName: 'Khách' }))
  })

  it('JoinPage_LoginAgainSuccess_ClearsFlag', async () => {
    saveAuth(auth(-1000))
    renderJoin()
    await screen.findByRole('dialog')

    fireEvent.click(screen.getByRole('button', { name: 'Đăng nhập lại' }))
    expect(await screen.findByText('login-from:/join?code=ABCDEF')).toBeTruthy() // quay lại đúng phòng sau khi đăng nhập
    expect(isSessionExpiredFlagSet()).toBe(true) // chưa đăng nhập xong thì cờ còn

    fireEvent.click(screen.getByRole('button', { name: 'probe-login' }))
    await waitFor(() => expect(isSessionExpiredFlagSet()).toBe(false))
  })

  it('JoinPage_TokenExpiresBetweenMountAndSubmit_BlocksSendAndShowsDialog', async () => {
    saveAuth(auth(60_000))
    renderJoin()
    await waitFor(() => expect(getMe).toHaveBeenCalled())
    expect(screen.queryByRole('dialog')).toBeNull()
    fillName('Alice')

    saveAuth(auth(-1000)) // token hết hạn trong lúc người dùng đang gõ
    submit()

    expect(await screen.findByRole('dialog', { name: 'Phiên đăng nhập đã hết hạn' })).toBeTruthy()
    expect(createPlayer).not.toHaveBeenCalled()
  })

  it('JoinPage_RedirectedWithNotice_ShowsNotice', () => {
    // PlayPage chuyển về /join kèm thông báo khi bị thiết bị khác thay thế (session_replaced).
    renderJoin({ notice: 'Bạn đã vào phòng từ thiết bị khác.' })

    expect(screen.getByRole('status').textContent).toBe('Bạn đã vào phòng từ thiết bị khác.')
  })

  it('JoinPage_NoNotice_ShowsNoStatus', () => {
    renderJoin()

    expect(screen.queryByRole('status')).toBeNull()
  })

  it('JoinPage_NoStoredAuth_GuestJoinsNormally', async () => {
    vi.mocked(createPlayer).mockResolvedValue({ playerId: 'p1', sessionToken: 's', roomCode: 'ABCDEF' })
    renderJoin()
    fillName('Khách')

    submit()

    await waitFor(() => expect(createPlayer).toHaveBeenCalledWith('ABCDEF', { displayName: 'Khách' }))
    expect(screen.queryByRole('dialog')).toBeNull()
  })
})

describe('JoinPage: tên hiển thị và thưởng', () => {
  it('JoinPage_LoggedIn_PrefillsDisplayNameEditableAndShowsRewardNote', async () => {
    saveAuth(auth(60_000))
    renderJoin()

    const input = (await screen.findByDisplayValue('Alice')) as HTMLInputElement
    expect(input.id).toBe('displayName')
    expect(input.disabled).toBe(false) // vẫn sửa được
    expect(screen.getByText(/XP và Xu được tính khi trận có ít nhất 2 tài khoản/)).toBeTruthy()
  })

  it('JoinPage_LoggedIn_RewardNoteDoesNotPromiseReward', async () => {
    saveAuth(auth(60_000))
    renderJoin()

    const note = await screen.findByText(/XP và Xu được tính khi/)
    expect(note.textContent).toContain('ít nhất 2 tài khoản') // nêu điều kiện, không hứa chắc
    expect(note.textContent).not.toMatch(/bạn sẽ nhận|chắc chắn/i)
  })

  it('JoinPage_Guest_ShowsGuestNoRewardHint', async () => {
    renderJoin()

    expect(await screen.findByText(/Vào làm khách sẽ không nhận XP và Xu/)).toBeTruthy()
    expect((screen.getByLabelText('Tên hiển thị') as HTMLInputElement).value).toBe('')
  })

  it('JoinPage_UserTypesNewName_UsesTypedName', async () => {
    saveAuth(auth(60_000))
    vi.mocked(createPlayer).mockResolvedValue({ playerId: 'p1', sessionToken: 's', roomCode: 'ABCDEF' })
    renderJoin()
    await screen.findByDisplayValue('Alice')

    fillName('Biệt danh')
    submit()

    await waitFor(() => expect(createPlayer).toHaveBeenCalledWith('ABCDEF', { displayName: 'Biệt danh' }))
  })
})
