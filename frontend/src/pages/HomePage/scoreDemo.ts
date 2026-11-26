/** Phản chiếu ScoreCalculator.Calculate ở backend (500 nền + tối đa 500 theo thời gian còn lại). Chỉ để minh họa; điểm thật tính ở server. */
export const DEMO_TIME_LIMIT_SECONDS = 20

export function demoScore(isCorrect: boolean, elapsedSeconds: number, timeLimitSeconds = DEMO_TIME_LIMIT_SECONDS): number {
  if (!isCorrect || elapsedSeconds > timeLimitSeconds) return 0
  const remaining = timeLimitSeconds - Math.max(0, elapsedSeconds)
  return 500 + Math.round((500 * remaining) / timeLimitSeconds)
}
