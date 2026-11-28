// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getMe } from '../../api/auth'
import { clearAuth, saveAuth } from '../../api/tokenStorage'
import { AuthProvider } from '../../components/AuthProvider'
import type { AuthResponse } from '../../types/auth'
import { SiteFooter } from './SiteFooter'
import source from './SiteFooter.tsx?raw'

vi.mock('../../api/auth', () => ({ getMe: vi.fn(), login: vi.fn(), register: vi.fn() }))

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }
const AUTH: AuthResponse = { token: 'jwt', expiresAt: new Date(Date.now() + 60_000).toISOString(), user: USER }

function renderFooter() {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <AuthProvider>
          <SiteFooter />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function href(name: string) {
  return screen.getByRole('link', { name }).getAttribute('href')
}

beforeEach(() => {
  clearAuth()
  vi.mocked(getMe).mockReset().mockResolvedValue(USER)
})
afterEach(cleanup)

describe('SiteFooter', () => {
  it('Source_Always_HasNoHugeWordmark', () => {
    expect(source).toContain('bg-ink')
    expect(source).not.toContain('text-[clamp(56px')
  })

  it('Render_LoggedOut_ShowsLinksWithRightTargets', () => {
    renderFooter()
    expect(href('Vào phòng')).toBe('/join')
    expect(href('Tạo phòng')).toBe('/register')
    expect(href('Cửa hàng')).toBe('/shop')
    expect(href('Xếp hạng')).toBe('/ranking')
    expect(href('Đăng nhập')).toBe('/login')
    expect(href('Đăng ký')).toBe('/register')
    expect(screen.queryByRole('link', { name: 'Bảng điều khiển' })).toBeNull()
  })

  it('Render_Always_GithubLinkOpensSafelyInNewTab', () => {
    renderFooter()
    const github = screen.getByRole('link', { name: 'GitHub' })
    expect(github.getAttribute('href')).toBe('https://github.com/anhquoc2005-eng/quizbattle')
    expect(github.getAttribute('target')).toBe('_blank')
    expect(github.getAttribute('rel')).toContain('noopener')
  })

  it('Render_LoggedIn_ShowsDashboardInsteadOfLogin', async () => {
    saveAuth(AUTH)
    renderFooter()
    await waitFor(() => expect(screen.getByRole('link', { name: 'Bảng điều khiển' })).toBeTruthy())
    expect(href('Tạo phòng')).toBe('/dashboard')
    expect(screen.queryByRole('link', { name: 'Đăng nhập' })).toBeNull()
  })
})
