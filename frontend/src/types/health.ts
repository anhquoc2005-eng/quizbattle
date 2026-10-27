/** Khớp với HealthResponse ở QuizBattle.Application/Health. */
export interface HealthResponse {
  status: 'ok' | 'degraded'
  database: 'up' | 'down'
}
