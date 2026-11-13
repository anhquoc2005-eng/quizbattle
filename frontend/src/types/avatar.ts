/** Tên ô của nhân vật (camelCase, trùng tên thành phần của DiceBear và khóa JSON của backend). */
export type AvatarSlot =
  | 'eyes'
  | 'eyebrows'
  | 'mouth'
  | 'nose'
  | 'hair'
  | 'clothes'
  | 'glasses'
  | 'beard'
  | 'gesture'
  | 'clothesGraphic'
  | 'background'

/** Khớp AvatarDto: mỗi ô là variantKey. Sáu ô bắt buộc luôn có giá trị; năm ô tùy chọn là null khi trống. */
export interface Avatar {
  eyes: string
  eyebrows: string
  mouth: string
  nose: string
  hair: string
  clothes: string
  glasses: string | null
  beard: string | null
  gesture: string | null
  clothesGraphic: string | null
  background: string | null
}

export type AvatarTier = 'free' | 'common' | 'rare' | 'special'

/** Khớp AvatarItemDto. key = "<ô>.<variantKey>". price là số Xu do server quy định (0 khi free). */
export interface AvatarItem {
  key: string
  slot: AvatarSlot
  variantKey: string
  name: string
  tier: AvatarTier
  price: number
}

/** Khớp MyAvatarDto. ownedItemKeys chỉ gồm món BÁN đã mua (món miễn phí luôn dùng được, không liệt kê). */
export interface MyAvatar {
  avatar: Avatar
  isDefault: boolean
  ownedItemKeys: string[]
}

/** Khớp SavedAvatarDto (kết quả PUT /api/users/me/avatar). */
export interface SavedAvatar {
  avatar: Avatar
  isDefault: boolean
}

/** Khớp PurchaseResultDto. */
export interface PurchaseResult {
  itemKey: string
  price: number
  coinBalance: number
}
