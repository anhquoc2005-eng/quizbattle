// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getMe, login as loginRequest } from '../../api/auth'
import { clearAuth, saveAuth } from '../../api/tokenStorage'
import { AuthProvider } from '../../components/AuthProvider'
import type { AuthResponse } from '../../types/auth'
import { LoginPage } from './LoginPage'

// Chỉ giả lập tầng gọi mạng; AuthProvider, tokenStorage và localStorage (jsdom) là thật.
vi.mock('../../api/auth', () => ({ getMe: vi.fn(), login: vi.fn(), register: vi.fn() }))

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }
const AUTH: AuthResponse = { token: 'jwt', expiresAt: new Date(Date.now() + 60_000).toISOString(), user: USER }

function LocationProbe({ label }: { label: string }) {
  const { pathname, search } = useLocation()
  return <p>{`${label}:${pathname}${search}`}</p>
}

function renderLogin(state?: unknown) {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { mutations: { retry: false } } })}>
      <MemoryRouter initialEntries={[{ pathname: '/login', state }]}>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/join" element={<LocationProbe label="at" />} />
            <Route path="/dashboard" element={<LocationProbe label="at" />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function signIn() {
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'a@b.c' } })
  fireEvent.change(screen.getByLabelText('Mật khẩu'), { target: { value: 'password123' } })
  fireEvent.submit(screen.getByRole('button', { name: 'Đăng nhập' }).closest('form')!)
}

beforeEach(() => {
  localStorage.clear()
  clearAuth()
  vi.mocked(getMe).mockReset().mockResolvedValue(USER)
  // Như mạng thật: kết quả tới ở một macrotask sau, ngoài lượt xử lý sự kiện của người dùng.
  vi.mocked(loginRequest).mockReset().mockImplementation(() => new Promise((resolve) => setTimeout(() => resolve(AUTH), 10)))
})
afterEach(cleanup)

describe('LoginPage: điều hướng sau khi đăng nhập', () => {
  it('LoginPage_LoginSuccessWithFromState_ReturnsToJoinWithCode', async () => {
    renderLogin({ from: { pathname: '/join', search: '?code=ABC123' } })
    await waitFor(() => expect(getMe).not.toHaveBeenCalled()) // chưa có token nên không gọi /me khi khởi động
    signIn()

    expect(await screen.findByText('at:/join?code=ABC123')).toBeTruthy()
  })

  it('LoginPage_AlreadyHasUserWithFromState_RedirectsToFromNotDashboard', async () => {
    // Nhánh "đã có user" của LoginPage chạy ngay khi login() gán user (trước hoặc sau onSuccess tùy thời điểm render),
    // nên đích của nó phải giống đích của onSuccess. Dựng thẳng trạng thái đó để không phụ thuộc thứ tự đua.
    saveAuth(AUTH)
    renderLogin({ from: { pathname: '/join', search: '?code=ABC123' } })

    expect(await screen.findByText('at:/join?code=ABC123')).toBeTruthy()
  })

  it('LoginPage_LoginSuccessWithoutFromState_GoesToDashboard', async () => {
    renderLogin()
    signIn()

    expect(await screen.findByText('at:/dashboard')).toBeTruthy()
  })
})
