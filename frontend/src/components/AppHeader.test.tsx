// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { Link, MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppHeader } from './AppHeader'
import type { AuthContextValue } from './AuthProvider'
import { AuthContext } from './authContext'

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }

function renderHeader(auth: Partial<AuthContextValue>, current?: 'practice' | 'shop') {
  const value = { user: null, isLoading: false, logout: vi.fn(), ...auth } as unknown as AuthContextValue
  return {
    value,
    ...render(
      <AuthContext.Provider value={value}>
        <MemoryRouter initialEntries={['/shop?x=1']}>
          <AppHeader current={current} />
          <Routes>
            <Route path="/login" element={<LoginProbe />} />
            <Route path="*" element={null} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    ),
  }
}

function LoginProbe() {
  const { state } = useLocation()
  return <Link to="/">{`from:${state?.from?.pathname}${state?.from?.search}`}</Link>
}

afterEach(cleanup)

describe('AppHeader', () => {
  it('AppHeader_SignedIn_ShowsNameLogoutAndMarksCurrentPage', () => {
    renderHeader({ user: USER }, 'practice')

    expect(screen.getByRole('link', { name: 'Alice' }).getAttribute('href')).toBe('/profile')
    expect(screen.getByRole('button', { name: 'Đăng xuất' })).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Ôn tập' }).getAttribute('aria-current')).toBe('page')
    expect(screen.getByRole('link', { name: 'Cửa hàng' }).getAttribute('aria-current')).toBeNull()
    expect(screen.queryByRole('link', { name: 'Đăng nhập' })).toBeNull()
  })

  it('AppHeader_SignedOut_ShowsLoginLinkCarryingCurrentPageAsFrom', () => {
    renderHeader({}, 'shop')

    expect(screen.queryByRole('button', { name: 'Đăng xuất' })).toBeNull()
    fireEvent.click(screen.getByRole('link', { name: 'Đăng nhập' }))
    expect(screen.getByText('from:/shop?x=1')).toBeTruthy()
  })

  it('AppHeader_AuthLoading_ShowsNeitherNameNorLogin', () => {
    renderHeader({ user: null, isLoading: true })

    expect(screen.queryByRole('link', { name: 'Đăng nhập' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Đăng xuất' })).toBeNull()
  })

  it('AppHeader_ClickLogout_CallsLogoutAndReloadsToHome', () => {
    const original = window.location
    Object.defineProperty(window, 'location', { configurable: true, value: { href: '/shop' } })
    try {
      const { value } = renderHeader({ user: USER })
      fireEvent.click(screen.getByRole('button', { name: 'Đăng xuất' }))

      expect(value.logout).toHaveBeenCalledTimes(1)
      expect(window.location.href).toBe('/')
    } finally {
      Object.defineProperty(window, 'location', { configurable: true, value: original })
    }
  })
})
