import type { ComponentProps } from 'react'
import type { Avatar } from './Avatar'

/**
 * Mật độ lưới lớp học theo số người chơi (hàm thuần, không phụ thuộc DOM). Mỗi bậc ứng với một --desk-min trong index.css
 * (.classroom[data-density=...]) và một cỡ nhân vật:
 *   roomy  (≤ 12 người): bàn 152px, nhân vật 96px
 *   medium (≤ 30 người): bàn 116px, nhân vật 64px
 *   dense  (≤ 50 người): bàn  96px, nhân vật 48px
 * Cách tính số cột xem chú thích ở .classroom trong index.css.
 */
export type ClassroomDensity = 'roomy' | 'medium' | 'dense'

export const ROOMY_MAX_PLAYERS = 12
export const MEDIUM_MAX_PLAYERS = 30

export const DESK_AVATAR_SIZE: Record<ClassroomDensity, ComponentProps<typeof Avatar>['size']> = {
  roomy: 'md',
  medium: 'tile-md',
  dense: 'tile-sm',
}

export function classroomDensity(playerCount: number): ClassroomDensity {
  if (playerCount <= ROOMY_MAX_PLAYERS) return 'roomy'
  if (playerCount <= MEDIUM_MAX_PLAYERS) return 'medium'
  return 'dense'
}
