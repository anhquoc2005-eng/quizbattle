import { useRef, useState } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../../api/apiError'
import { describeAvatarError } from '../../api/avatarErrors'
import { GUEST_PREVIEW_AVATAR } from '../../avatar/guestPreview'
import { AVATAR_SLOTS, SLOT_LABELS } from '../../avatar/avatarSlots'
import { previewWith } from '../../avatar/preview'
import { CoinBalance } from '../../components/CoinBalance'
import { ErrorRetry } from '../../components/ErrorRetry'
import { LogoMark } from '../../components/LogoMark'
import { useAuth } from '../../hooks/useAuth'
import { useMyAvatar } from '../../hooks/useMyAvatar'
import { useProfile } from '../../hooks/useProfile'
import { usePurchase } from '../../hooks/usePurchase'
import { useShopItems } from '../../hooks/useShopItems'
import type { AvatarItem } from '../../types/avatar'
import { ShopItemCard } from './ShopItemCard'
import { ShopPreview } from './ShopPreview'

/** Cửa hàng: xem danh mục công khai; mua cần đăng nhập. Số dư và món đã sở hữu do server quyết. */
export function ShopPage() {
  const { user, isLoading: authLoading } = useAuth()
  const signedIn = user !== null
  const catalog = useShopItems()
  const mine = useMyAvatar(signedIn)
  const profile = useProfile(signedIn)
  const purchase = usePurchase()
  const buying = useRef(false) // isPending của TanStack cập nhật chậm một nhịp: chặn bấm đúp đồng bộ
  const [previewKey, setPreviewKey] = useState<string | null>(null)

  const privateQueries = signedIn ? [mine, profile] : []
  const loading = authLoading || catalog.isPending || privateQueries.some((q) => q.isPending)
  const failed = [catalog, ...privateQueries].find((q) => q.isError)

  const forSale = (catalog.data ?? []).filter((item) => item.tier !== 'free')
  const owned = new Set(mine.data?.ownedItemKeys ?? [])
  const base = signedIn && mine.data ? mine.data.avatar : GUEST_PREVIEW_AVATAR
  const previewItem = forSale.find((item) => item.key === previewKey) ?? null
  const preview = previewItem ? previewWith(base, previewItem) : null

  // already_owned (bấm hai lần) không phải lỗi với người dùng: sau khi làm mới thì món hiện "Đã sở hữu".
  const purchaseError =
    purchase.isError && !(purchase.error instanceof ApiError && purchase.error.problem?.code === 'already_owned')
      ? describeAvatarError(purchase.error)
      : null

  function buy(item: AvatarItem) {
    if (buying.current) return
    buying.current = true
    purchase.mutate(item.key, { onSettled: () => { buying.current = false } })
  }

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <Link to="/ranking">Xếp hạng</Link>
          {signedIn ? <Link to="/profile">{user.displayName}</Link> : <Link to="/login">Đăng nhập</Link>}
        </nav>
      </header>

      <main className="col grid gap-3.5 py-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h1>Cửa hàng</h1>
          {signedIn && profile.data && <CoinBalance coins={profile.data.coinBalance} />}
        </div>

        {loading && <p className="text-muted">Đang tải…</p>}
        {!loading && failed && (
          <ErrorRetry
            message={describeAvatarError(failed.error)}
            onRetry={() => [catalog, ...privateQueries].forEach((q) => void q.refetch())}
          />
        )}
        {!loading && !failed && forSale.length === 0 && <p className="text-muted">Cửa hàng chưa có món nào để bán.</p>}

        {!loading && !failed && forSale.length > 0 && (
          <>
            <ShopPreview base={base} preview={preview} item={previewItem} onClear={() => setPreviewKey(null)} />
            {purchaseError && <p role="alert" className="text-negative">{purchaseError}</p>}
            {AVATAR_SLOTS.map((slot) => {
              const items = forSale.filter((item) => item.slot === slot)
              if (items.length === 0) return null
              return (
                <section key={slot} className="card grid gap-3" aria-label={SLOT_LABELS[slot]}>
                  <h2>{SLOT_LABELS[slot]}</h2>
                  <ul className="m-0 grid list-none gap-2 p-0 sm:grid-cols-2">
                    {items.map((item) => (
                      <ShopItemCard
                        key={item.key}
                        item={item}
                        owned={owned.has(item.key)}
                        signedIn={signedIn}
                        coins={profile.data?.coinBalance ?? null}
                        buying={purchase.isPending && purchase.variables === item.key}
                        busy={purchase.isPending}
                        previewing={previewKey === item.key}
                        onPreview={() => setPreviewKey(previewKey === item.key ? null : item.key)}
                        onBuy={() => buy(item)}
                      />
                    ))}
                  </ul>
                </section>
              )
            })}
          </>
        )}
      </main>
    </>
  )
}
