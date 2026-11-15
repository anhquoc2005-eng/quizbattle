import type { Avatar } from '../types/avatar'
import { AVATAR_SLOTS, DEFAULT_BACKGROUND_HEX } from './avatarSlots'

/** Chữ ký ổn định của nhân vật: cùng nhân vật cùng chuỗi. Dùng làm khóa bộ nhớ đệm và làm `seed` (chỉ ảnh hưởng hậu tố id của SVG). */
export function signature(avatar: Avatar): string {
  return AVATAR_SLOTS.map((slot) => `${slot}.${avatar[slot] ?? ''}`).join('|')
}

/**
 * Tùy chọn cho DiceBear: ô có món thì ép đúng variant (`<ô>Variant` + `<ô>Probability: 100`), ô trống thì tắt (`Probability: 0`).
 * Nền là mã hex không có '#'; để trống thì dùng nền mặc định. Hàm thuần, không phụ thuộc DiceBear.
 */
export function buildOptions(avatar: Avatar): Record<string, string | number | string[]> {
  const options: Record<string, string | number | string[]> = {
    seed: signature(avatar),
    backgroundColor: [avatar.background ?? DEFAULT_BACKGROUND_HEX],
  }

  for (const slot of AVATAR_SLOTS) {
    if (slot === 'background') continue
    const variant = avatar[slot]
    if (variant === null) {
      options[`${slot}Probability`] = 0
    } else {
      options[`${slot}Variant`] = variant
      options[`${slot}Probability`] = 100
    }
  }

  return options
}
