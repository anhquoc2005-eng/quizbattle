/** Bảng ký tự mã phòng, khớp RoomCodeGenerator.Alphabet ở backend (bỏ I, L, O, 0, 1 cho khỏi nhầm). */
export const ROOM_CODE_CHARS = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789'
export const ROOM_CODE_LENGTH = 6

/** Viết hoa, bỏ ký tự ngoài bảng, cắt còn tối đa 6 ký tự. */
export function sanitizeRoomCode(raw: string): string {
  return [...raw.toUpperCase()].filter((c) => ROOM_CODE_CHARS.includes(c)).slice(0, ROOM_CODE_LENGTH).join('')
}

export function isCompleteRoomCode(code: string): boolean {
  return code.length === ROOM_CODE_LENGTH && sanitizeRoomCode(code) === code
}
