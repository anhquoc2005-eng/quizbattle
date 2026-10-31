/** Khớp với UserDto ở QuizBattle.Application/Auth. */
export interface AuthUser {
  id: string
  email: string
  displayName: string
}

/** Khớp với AuthResponse ở QuizBattle.Application/Auth. */
export interface AuthResponse {
  token: string
  expiresAt: string
  user: AuthUser
}
