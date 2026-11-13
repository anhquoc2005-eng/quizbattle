import { useEffect, useState } from 'react'
import { signature } from '../avatar/avatarOptions'
import { peekAvatarDataUri, renderAvatarDataUri } from '../avatar/avatarRenderer'
import type { Avatar } from '../types/avatar'

/**
 * Data URI của nhân vật, hoặc undefined khi chưa sẵn sàng (đang nạp DiceBear lần đầu, hoặc nạp lỗi: component hiện khung chờ).
 * Có trong bộ nhớ đệm thì trả ngay, không qua trạng thái chờ. Không bao giờ ném lỗi ra ngoài.
 */
export function useAvatarImage(avatar: Avatar): string | undefined {
  const sig = signature(avatar)
  const cached = peekAvatarDataUri(avatar)
  const [loaded, setLoaded] = useState<{ sig: string; uri: string } | null>(null)

  useEffect(() => {
    if (cached !== undefined) return
    let cancelled = false
    renderAvatarDataUri(avatar)
      .then((uri) => {
        if (!cancelled) setLoaded({ sig, uri })
      })
      .catch(() => undefined) // lỗi nạp chunk: giữ khung chờ, lần render sau sẽ thử lại
    return () => {
      cancelled = true
    }
  }, [avatar, cached, sig])

  return cached ?? (loaded?.sig === sig ? loaded.uri : undefined)
}
