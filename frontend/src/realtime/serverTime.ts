// Đồng hồ điện thoại người chơi có thể lệch vài giây so với server, nên không tính thời gian còn lại
// bằng deadline - Date.now() trực tiếp. Thay vào đó: tính offset = serverTime - Date.now() lúc nhận sự
// kiện (RoomState/QuestionStarted có ServerTime từ server), rồi suy ra "giờ server bây giờ" = Date.now() + offset.

/** offset = serverTime - Date.now(), tính tại thời điểm NHẬN sự kiện mang serverTime. */
export function computeClockOffsetMs(serverTimeIso: string, now = Date.now()): number {
  return new Date(serverTimeIso).getTime() - now
}

/** Thời gian còn lại tới deadline (mili giây), dùng offset đã tính để bù lệch đồng hồ. Không bao giờ âm. */
export function getRemainingMs(deadlineIso: string, offsetMs: number, now = Date.now()): number {
  const estimatedServerNow = now + offsetMs
  return Math.max(0, new Date(deadlineIso).getTime() - estimatedServerNow)
}

/** Số giây còn lại (làm tròn lên) tới một mốc server, dùng cho "Câu tiếp theo sau Ns". */
export function getRemainingSeconds(targetIso: string, offsetMs: number, now = Date.now()): number {
  return Math.ceil(getRemainingMs(targetIso, offsetMs, now) / 1000)
}
