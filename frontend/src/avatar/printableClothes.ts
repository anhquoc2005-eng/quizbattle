/**
 * Kiểu áo có chỗ in hình (đo bằng cách vẽ từng áo có/không hình in trên DiceBear 10.6.0). Khớp
 * AvatarCatalog.PrintableClothesVariantKeys ở backend. Test `printableClothes.test.ts` dựng lại tập này từ đúng phiên bản đã ghim
 * để nâng cấp DiceBear không làm hằng số lệch.
 */
export const PRINTABLE_CLOTHES: readonly string[] = [
  'variant01',
  'variant02',
  'variant03',
  'variant04',
  'variant05',
  'variant06',
  'variant07',
  'variant08',
  'variant09',
  'variant10',
]

export function isPrintable(clothesVariantKey: string): boolean {
  return PRINTABLE_CLOTHES.includes(clothesVariantKey)
}
