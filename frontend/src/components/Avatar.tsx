import { useAvatarImage } from '../hooks/useAvatarImage'
import type { Avatar as AvatarData } from '../types/avatar'

// Cỡ cố định: nhỏ (28 px) cạnh tên người chơi, lớn (160 px) ở hồ sơ và cửa hàng. Khung bo góc cùng bán kính với .card cho lớn.
const SIZE_CLASS = { sm: 'size-7 rounded-lg', lg: 'size-40 rounded-[20px]' } as const

interface AvatarProps {
  avatar: AvatarData
  size: keyof typeof SIZE_CLASS
  /** Mô tả cho trình đọc màn hình. Bỏ trống = hình trang trí (thường cạnh tên đã có chữ). */
  label?: string
}

/**
 * Nhân vật vẽ từ DTO của server (không tự sinh ở client). Vẽ bằng <img src=data URI> nên SVG không bao giờ được chèn vào DOM.
 * Trong lúc nạp DiceBear lần đầu (hoặc nạp lỗi) hiện khung xám cùng cỡ để bố cục không nhảy.
 */
export function Avatar({ avatar, size, label }: AvatarProps) {
  const src = useAvatarImage(avatar)
  const frame = `${SIZE_CLASS[size]} shrink-0`

  if (src === undefined) {
    return <span data-state="loading" className={`${frame} inline-block bg-raised`} />
  }

  return <img className={`${frame} bg-raised object-cover`} src={src} alt={label ?? ''} aria-hidden={label ? undefined : true} />
}
