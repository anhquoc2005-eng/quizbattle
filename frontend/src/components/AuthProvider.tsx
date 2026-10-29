import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { getMe, login as loginRequest, register as registerRequest } from '../api/auth'
import { setUnauthorizedHandler } from '../api/client'
import { clearAuth, loadAuth, saveAuth } from '../api/tokenStorage'
import type { AuthUser } from '../types/auth'

export interface AuthContextValue {
  user: AuthUser | null
  /** true khi đang xác nhận phiên đăng nhập lúc khởi động (gọi GET /me). */
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const logout = useCallback(() => {
    clearAuth()
    setUser(null)
  }, [])

  // Request nào đó đã gắn token mà bị 401: phiên hết dùng được, chỉ cần đặt user = null.
  // ProtectedRoute tự chuyển về /login khi cần; trang công khai thì không bị ảnh hưởng.
  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null))
    return () => setUnauthorizedHandler(null)
  }, [])

  useEffect(() => {
    const stored = loadAuth()
    if (!stored) {
      setIsLoading(false)
      return
    }

    getMe()
      .then(setUser)
      .catch(() => {
        clearAuth()
        setUser(null)
      })
      .finally(() => setIsLoading(false))
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const result = await loginRequest({ email, password })
    saveAuth(result)
    setUser(result.user)
  }, [])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const result = await registerRequest({ email, password, displayName })
    saveAuth(result)
    setUser(result.user)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
