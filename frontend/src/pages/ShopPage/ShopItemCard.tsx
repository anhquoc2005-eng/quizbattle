import { Link } from 'react-router'
import { TIER_LABELS } from '../../avatar/avatarSlots'
import type { AvatarItem } from '../../types/avatar'

interface ShopItemCardProps {
  item: AvatarItem
  owned: boolean
  signedIn: boolean
  /** Số dư hiện tại; null khi chưa đăng nhập. */
  coins: number | null
  /** Đang mua chính món này. */
  buying: boolean
  /** Đang mua một món nào đó (khóa mọi nút Mua để không gửi hai request). */
  busy: boolean
  previewing: boolean
  onPreview: () => void
  onBuy: () => void
}

/** Một món trong cửa hàng: tên, mức và giá, nút xem thử và nút mua (hoặc "Đã sở hữu" / "Đăng nhập để mua"). */
export function ShopItemCard({ item, owned, signedIn, coins, buying, busy, previewing, onPreview, onBuy }: ShopItemCardProps) {
  const cannotAfford = coins !== null && coins < item.price

  return (
    <li className="flex flex-wrap items-center justify-between gap-3 rounded-[14px] bg-raised p-3">
      <div className="grid gap-0.5">
        <span className="font-medium">{item.name}</span>
        <span className="text-xs tabular-nums text-muted">
          {TIER_LABELS[item.tier]} · {item.price} Xu
        </span>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <button className="btn btn-ghost" type="button" aria-pressed={previewing} onClick={onPreview}>
          Xem thử
        </button>
        {owned ? (
          <span className="text-sm text-positive">Đã sở hữu</span>
        ) : !signedIn ? (
          <Link className="btn btn-ghost" to="/login">
            Đăng nhập để mua
          </Link>
        ) : (
          <button className="btn btn-primary" type="button" disabled={busy || cannotAfford} onClick={onBuy}>
            {buying ? 'Đang mua…' : cannotAfford ? 'Không đủ Xu' : `Mua · ${item.price} Xu`}
          </button>
        )}
      </div>
    </li>
  )
}
