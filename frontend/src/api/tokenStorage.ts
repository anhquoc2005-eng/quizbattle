import type { AuthResponse } from '../types/auth'

const STORAGE_KEY = 'quizbattle.auth'

/** true nếu đã qua hạn (hoặc expiresAt không đọc được). */
export function isTokenExpired(expiresAt: string): boolean {
  const expiry = new Date(expiresAt).getTime()
  return Number.isNaN(expiry) || expiry <= Date.now()
}

/** Đọc phiên đăng nhập đã lưu. Hết hạn thì xóa luôn và trả về null. localStorage có thể bị chặn (chế độ riêng tư...). */
export function loadAuth(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return null
    }

    const parsed = JSON.parse(raw) as AuthResponse
    if (isTokenExpired(parsed.expiresAt)) {
      clearAuth()
      return null
    }

    return parsed
  } catch {
    return null
  }
}

export function saveAuth(auth: AuthResponse): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
  } catch {
    // localStorage bị chặn: coi như không lưu được, người dùng phải đăng nhập lại ở lần tải sau.
  }
}

export function clearAuth(): void {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}
