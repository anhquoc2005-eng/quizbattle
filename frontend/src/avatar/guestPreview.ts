import type { Avatar } from '../types/avatar'

/**
 * Nhân vật nền để khách (chưa đăng nhập) xem thử món ở cửa hàng: một hằng số cố định chỉ gồm món miễn phí
 * (kiểu 01 của mọi ô bắt buộc đều miễn phí) trên nền xám mặc định. Không phải nhân vật tự sinh theo người dùng:
 * nhân vật mặc định của từng người luôn do server tính.
 */
export const GUEST_PREVIEW_AVATAR: Avatar = {
  eyes: 'variant01',
  eyebrows: 'variant01',
  mouth: 'variant01',
  nose: 'variant01',
  hair: 'variant01',
  clothes: 'variant01',
  glasses: null,
  beard: null,
  gesture: null,
  clothesGraphic: null,
  background: 'd4d4d4',
}
