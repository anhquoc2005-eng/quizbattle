import { Avatar } from '../../components/Avatar'
import type { Preview } from '../../avatar/preview'
import type { Avatar as AvatarData, AvatarItem } from '../../types/avatar'

interface ShopPreviewProps {
  base: AvatarData
  preview: Preview | null
  item: AvatarItem | null
  onClear: () => void
}

/** Khung xem thử: nhân vật của bạn (hoặc nhân vật mặc định của khách) khi mặc thử món đang chọn. Không đổi dữ liệu đã lưu. */
export function ShopPreview({ base, preview, item, onClear }: ShopPreviewProps) {
  return (
    <div className="card flex flex-wrap items-center gap-4">
      <Avatar avatar={preview?.avatar ?? base} size="lg" label="Xem thử nhân vật" />
      <div className="grid gap-2">
        <p className="text-muted">{item ? `Đang xem thử: ${item.name}` : 'Chọn "Xem thử" ở một món để mặc thử lên nhân vật.'}</p>
        {preview?.note && <p className="text-sm text-faint">{preview.note}</p>}
        {item && (
          <button className="btn btn-ghost justify-self-start" type="button" onClick={onClear}>
            Bỏ xem thử
          </button>
        )}
      </div>
    </div>
  )
}
