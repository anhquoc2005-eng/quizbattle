import type { AvatarItem, AvatarSlot } from '../types/avatar'

/** Món người dùng được mặc ở một ô: món miễn phí và món bán đã sở hữu (không bao giờ có món bán chưa mua). Giữ thứ tự của danh mục. */
export function availableItems(items: readonly AvatarItem[], ownedItemKeys: readonly string[], slot: AvatarSlot): AvatarItem[] {
  const owned = new Set(ownedItemKeys)
  return items.filter((item) => item.slot === slot && (item.tier === 'free' || owned.has(item.key)))
}
