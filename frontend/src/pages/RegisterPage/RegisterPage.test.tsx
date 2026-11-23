// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getMe, register as registerRequest } from '../../api/auth'
import { clearAuth } from '../../api/tokenStorage'
import { AuthProvider } from '../../components/AuthProvider'
import type { AuthResponse } from '../../types/auth'
import { RegisterPage } from './RegisterPage'

vi.mock('../../api/auth', () => ({ getMe: vi.fn(), login: vi.fn(), register: vi.fn() }))

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }
const AUTH: AuthResponse = { token: 'jwt', expiresAt: new Date(Date.now() + 60_000).toISOString(), user: USER }

function LocationProbe() {
  const { pathname, search } = useLocation()
  return <p>{`at:${pathname}${search}`}</p>
}

function renderRegister(state?: unknown) {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { mutations: { retry: false } } })}>
      <MemoryRouter initialEntries={[{ pathname: '/register', state }]}>
        <AuthProvider>
          <Routes>
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/practice" element={<LocationProbe />} />
            <Route path="/dashboard" element={<LocationProbe />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function submit() {
  fireEvent.change(screen.getByLabelText('Tên hiển thị'), { target: { value: 'Alice' } })
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'a@b.c' } })
  fireEvent.change(screen.getByLabelText('Mật khẩu'), { target: { value: 'password123' } })
  fireEvent.submit(screen.getByRole('button', { name: 'Đăng ký' }).closest('form')!)
}

beforeEach(() => {
  localStorage.clear()
  clearAuth()
  vi.mocked(getMe).mockReset().mockResolvedValue(USER)
  vi.mocked(registerRequest).mockReset().mockImplementation(() => new Promise((resolve) => setTimeout(() => resolve(AUTH), 10)))
})
afterEach(cleanup)

describe('RegisterPage: điều hướng sau khi đăng ký', () => {
  it('RegisterPage_SuccessWithFromState_ReturnsToPracticeAndRegistersOnce', async () => {
    renderRegister({ from: { pathname: '/practice' } })
    submit()

    expect(await screen.findByText('at:/practice')).toBeTruthy()
    expect(registerRequest).toHaveBeenCalledTimes(1)
  })

  it('RegisterPage_SuccessWithoutFromState_GoesToDashboard', async () => {
    renderRegister()
    submit()

    expect(await screen.findByText('at:/dashboard')).toBeTruthy()
    expect(registerRequest).toHaveBeenCalledTimes(1)
  })
})
