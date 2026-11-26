// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getHealth } from '../../api/health'
import { getMe } from '../../api/auth'
import { clearAuth, saveAuth } from '../../api/tokenStorage'
import { AuthProvider } from '../../components/AuthProvider'
import type { AuthResponse } from '../../types/auth'
import { HomePage } from './HomePage'

vi.mock('../../api/health', () => ({ getHealth: vi.fn() }))
vi.mock('../../api/auth', () => ({ getMe: vi.fn(), login: vi.fn(), register: vi.fn() }))

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }
const AUTH: AuthResponse = { token: 'jwt', expiresAt: new Date(Date.now() + 60_000).toISOString(), user: USER }

function renderHome() {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <AuthProvider>
          <HomePage />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  clearAuth()
  vi.mocked(getHealth).mockReset().mockRejectedValue(new Error('down'))
  vi.mocked(getMe).mockReset().mockResolvedValue(USER)
})
afterEach(cleanup)

describe('HomePage', () => {
  it('Render_Always_HasNoSystemStatusCardAndPingsHealthOnce', async () => {
    renderHome()
    await waitFor(() => expect(getHealth).toHaveBeenCalledTimes(1))
    expect(screen.queryByText('Trạng thái hệ thống')).toBeNull()
  })

  it('Render_LoggedOut_ShowsAuthAndSharedLinks', async () => {
    renderHome()
    for (const name of ['Đăng nhập', 'Đăng ký', 'Vào phòng', 'Cửa hàng', 'Xếp hạng']) {
      expect(await screen.findByRole('link', { name })).toBeTruthy()
    }
  })

  it('Render_LoggedIn_ShowsDashboardLinkInsteadOfLogin', async () => {
    saveAuth(AUTH)
    renderHome()
    expect(await screen.findByRole('link', { name: 'Vào trang của tôi' })).toBeTruthy()
    expect(screen.queryByRole('link', { name: 'Đăng nhập' })).toBeNull()
  })

  it('Render_Always_H1HasAccessibleNameQuizBattle', () => {
    renderHome()
    expect(screen.getByRole('heading', { level: 1, name: 'QuizBattle' })).toBeTruthy()
  })

  it('Render_Always_DemoCardIsHiddenFromAssistiveTech', () => {
    renderHome()
    expect(screen.getByText('Thủ đô của Úc là?').closest('.card')?.getAttribute('aria-hidden')).toBe('true')
  })
})
