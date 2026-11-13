import type { AvatarSlot, AvatarTier } from '../types/avatar'

/** Thứ tự cố định của các ô (cũng là thứ tự hiển thị trong trình chỉnh và thứ tự trong chữ ký nhân vật). */
export const AVATAR_SLOTS: readonly AvatarSlot[] = [
  'eyes',
  'eyebrows',
  'mouth',
  'nose',
  'hair',
  'clothes',
  'glasses',
  'beard',
  'gesture',
  'clothesGraphic',
  'background',
]

/** Sáu ô luôn phải có món. */
export const REQUIRED_SLOTS: readonly AvatarSlot[] = ['eyes', 'eyebrows', 'mouth', 'nose', 'hair', 'clothes']

/** Năm ô được để trống. */
export const OPTIONAL_SLOTS: readonly AvatarSlot[] = ['glasses', 'beard', 'gesture', 'clothesGraphic', 'background']

export function isRequiredSlot(slot: AvatarSlot): boolean {
  return REQUIRED_SLOTS.includes(slot)
}

export const SLOT_LABELS: Record<AvatarSlot, string> = {
  eyes: 'Mắt',
  eyebrows: 'Lông mày',
  mouth: 'Miệng',
  nose: 'Mũi',
  hair: 'Tóc',
  clothes: 'Áo',
  glasses: 'Kính',
  beard: 'Râu',
  gesture: 'Cử chỉ',
  clothesGraphic: 'Hình in',
  background: 'Màu nền',
}

export const TIER_LABELS: Record<AvatarTier, string> = {
  free: 'Miễn phí',
  common: 'Thường',
  rare: 'Hiếm',
  special: 'Đặc biệt',
}

/**
 * Nền khi ô "màu nền" để trống: xám mặc định. Khớp AvatarCatalog.DefaultBackgroundHex ở backend (test backend ghim hằng đó).
 * Không có nền thì SVG trong suốt và nét đen mất trên giao diện nền đen.
 */
export const DEFAULT_BACKGROUND_HEX = 'd4d4d4'
