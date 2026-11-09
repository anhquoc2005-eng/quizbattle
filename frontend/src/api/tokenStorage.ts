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
      // Ghi nhớ "phiên vừa hết hạn" để JoinPage báo thay vì âm thầm coi người dùng là khách (FR-021).
      markSessionExpired()
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

// --- Cờ "phiên đăng nhập vừa hết hạn" ---
// Dùng localStorage (không phải sessionStorage) để tab đang mở sẵn cũng thấy cờ qua sự kiện `storage`. Biến trong bộ
// nhớ là phương án dự phòng khi localStorage bị chặn (chế độ riêng tư) - khi đó chỉ có hiệu lực trong tab này.
const EXPIRED_FLAG_KEY = 'quizbattle.sessionExpired'
let expiredFlagInMemory = false
const expiredListeners = new Set<() => void>()

function notifyExpiredListeners(): void {
  expiredListeners.forEach((listener) => listener())
}

export function markSessionExpired(): void {
  expiredFlagInMemory = true
  try {
    localStorage.setItem(EXPIRED_FLAG_KEY, '1')
  } catch {
    // Chỉ còn cờ trong bộ nhớ.
  }
  notifyExpiredListeners()
}

export function isSessionExpiredFlagSet(): boolean {
  try {
    return localStorage.getItem(EXPIRED_FLAG_KEY) !== null || expiredFlagInMemory
  } catch {
    return expiredFlagInMemory
  }
}

export function clearSessionExpiredFlag(): void {
  expiredFlagInMemory = false
  try {
    localStorage.removeItem(EXPIRED_FLAG_KEY)
  } catch {
    // ignore
  }
  notifyExpiredListeners()
}

/** Đăng ký nghe thay đổi cờ (trong tab này và từ tab khác qua sự kiện storage). Trả về hàm hủy đăng ký. */
export function subscribeSessionExpired(listener: () => void): () => void {
  expiredListeners.add(listener)
  const onStorage = (event: StorageEvent) => {
    if (event.key === EXPIRED_FLAG_KEY || event.key === null) listener()
  }
  window.addEventListener('storage', onStorage)
  return () => {
    expiredListeners.delete(listener)
    window.removeEventListener('storage', onStorage)
  }
}
