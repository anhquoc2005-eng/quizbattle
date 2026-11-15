import type { StyleDefinition } from '@dicebear/core'
import type { Avatar } from '../types/avatar'
import { buildOptions, signature } from './avatarOptions'

/**
 * NƠI DUY NHẤT nạp DiceBear: `@dicebear/core` và `notionists.json` (372 KB thô, ≈ 111 KB gzip) đều qua `import()` động nên Vite
 * tách chúng thành chunk riêng, không nằm trong bundle chính; chỉ tải khi màn đầu tiên cần vẽ nhân vật. Không dùng API
 * HTTP công khai của DiceBear (xem CLAUDE.md và THIRD_PARTY_NOTICES.md). Không bao giờ chèn SVG thô vào DOM: kết quả là data URI cho `<img>`.
 */
type Render = (avatar: Avatar) => string

let renderPromise: Promise<Render> | null = null

function loadRender(): Promise<Render> {
  renderPromise ??= Promise.all([import('@dicebear/core'), import('@dicebear/styles/notionists.json')])
    .then(([core, definition]) => {
      // Ép kiểu một lần, ở đây: file JSON khớp StyleDefinition lúc chạy nhưng TypeScript không suy ra được.
      const style = new core.Style(definition.default as unknown as StyleDefinition)
      return (avatar: Avatar) => new core.Avatar(style, buildOptions(avatar) as never).toDataUri()
    })
    .catch((error: unknown) => {
      renderPromise = null // cho phép thử lại ở lần gọi sau (ví dụ mạng rớt khi tải chunk)
      throw error
    })
  return renderPromise
}

// Bộ nhớ đệm theo chữ ký nhân vật: nhiều người trong phòng dùng chung nhân vật mặc định nên số lần vẽ thật thấp.
// shortcut: giới hạn đơn giản (xóa mục cũ nhất), nâng lên LRU thật nếu đo thấy vẽ lại quá nhiều.
const MAX_CACHED = 200
const cache = new Map<string, string>()

/** Data URI đã vẽ sẵn (nếu có), để component hiện ngay không phải chờ. */
export function peekAvatarDataUri(avatar: Avatar): string | undefined {
  return cache.get(signature(avatar))
}

/** Vẽ nhân vật thành data URI SVG (tải DiceBear lần đầu nếu cần). Cùng nhân vật chỉ vẽ một lần. */
export async function renderAvatarDataUri(avatar: Avatar): Promise<string> {
  const key = signature(avatar)
  const cached = cache.get(key)
  if (cached !== undefined) return cached

  const render = await loadRender()
  const uri = render(avatar)
  cache.set(key, uri)
  if (cache.size > MAX_CACHED) {
    const oldest = cache.keys().next().value
    if (oldest !== undefined) cache.delete(oldest)
  }
  return uri
}
