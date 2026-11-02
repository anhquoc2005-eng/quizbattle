/** Phiên người chơi (không cần tài khoản), lưu riêng theo từng mã phòng để tải lại trang hoặc trình
 * duyệt điện thoại tự đóng tab vẫn vào lại được đúng phòng, giữ nguyên điểm. */
export interface PlayerSession {
  playerId: string
  sessionToken: string
}

function storageKey(code: string): string {
  return `quizbattle.player.${code}`
}

export function savePlayerSession(code: string, session: PlayerSession): void {
  try {
    localStorage.setItem(storageKey(code), JSON.stringify(session))
  } catch {
    // localStorage bị chặn (chế độ riêng tư...): coi như không lưu được, người chơi phải vào lại bằng tay.
  }
}

export function loadPlayerSession(code: string): PlayerSession | null {
  try {
    const raw = localStorage.getItem(storageKey(code))
    if (!raw) return null
    const parsed: unknown = JSON.parse(raw)
    if (isPlayerSession(parsed)) return { playerId: parsed.playerId, sessionToken: parsed.sessionToken }
  } catch {
    // JSON hỏng hoặc localStorage bị chặn - rơi xuống xóa bên dưới.
  }
  // Dữ liệu sai dạng (bản cũ, bị sửa tay...): xóa để lần sau không đọc lại, người chơi vào lại qua /join.
  clearPlayerSession(code)
  return null
}

function isPlayerSession(value: unknown): value is PlayerSession {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as Record<string, unknown>).playerId === 'string' &&
    typeof (value as Record<string, unknown>).sessionToken === 'string'
  )
}

export function clearPlayerSession(code: string): void {
  try {
    localStorage.removeItem(storageKey(code))
  } catch {
    // ignore
  }
}
