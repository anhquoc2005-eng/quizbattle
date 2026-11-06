import type { AuthResponse, AuthUser } from '../types/auth'
import { ApiError } from './apiError'

/**
 * Xác nhận phiên đã lưu lúc khởi động. Chỉ xóa phiên khi server từ chối token (401).
 * Lỗi mạng/5xx thì giữ token: host miễn phí hay ngủ hoặc khởi động lại, và nhiều tab
 * dùng chung localStorage nên xóa ở đây sẽ làm tab đang chơi mất phiên theo.
 */
export async function restoreSession(
  stored: AuthResponse,
  fetchMe: () => Promise<AuthUser>,
  clear: () => void,
): Promise<AuthUser | null> {
  try {
    return await fetchMe()
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      clear()
      return null
    }
    return stored.user
  }
}
