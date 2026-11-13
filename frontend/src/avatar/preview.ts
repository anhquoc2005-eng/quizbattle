import type { Avatar, AvatarItem } from '../types/avatar'
import { isPrintable } from './printableClothes'

export interface Preview {
  avatar: Avatar
  /** Giải thích khi xem thử phải đổi thêm một món khác (ví dụ áo), null nếu không có gì đặc biệt. */
  note: string | null
}

const PRINTABLE_SHIRT = 'variant01'

/**
 * Nhân vật khi mặc thử một món lên `base` (không đụng dữ liệu đã lưu, không tốn Xu). Hình in trên áo không in được thì
 * đổi sang áo 01 để người xem thấy được hình và kèm ghi chú.
 */
export function previewWith(base: Avatar, item: AvatarItem): Preview {
  const avatar: Avatar = { ...base, [item.slot]: item.variantKey }

  if (item.slot === 'clothesGraphic' && !isPrintable(avatar.clothes)) {
    return {
      avatar: { ...avatar, clothes: PRINTABLE_SHIRT },
      note: 'Đã đổi sang áo 01 để xem hình in (áo đang mặc không in được hình).',
    }
  }

  return { avatar, note: null }
}
