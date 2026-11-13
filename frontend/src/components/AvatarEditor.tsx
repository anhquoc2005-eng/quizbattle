import { useRef, useState } from 'react'
import { describeAvatarError } from '../api/avatarErrors'
import { availableItems } from '../avatar/availableItems'
import { signature } from '../avatar/avatarOptions'
import { AVATAR_SLOTS } from '../avatar/avatarSlots'
import { isPrintable } from '../avatar/printableClothes'
import { useMyAvatar, useSaveAvatar } from '../hooks/useMyAvatar'
import { useShopItems } from '../hooks/useShopItems'
import type { Avatar as AvatarData, AvatarItem, MyAvatar } from '../types/avatar'
import { Avatar } from './Avatar'
import { AvatarSlotPicker } from './AvatarSlotPicker'
import { ErrorRetry } from './ErrorRetry'

const GRAPHIC_HINT = 'Hình in chỉ dùng được với áo 01 đến 10. Hãy đổi áo để chọn hình in.'
const GRAPHIC_CONFLICT = 'Áo đang chọn không in được hình: đổi sang áo 01 đến 10 hoặc bỏ hình in.'

/** Trình chỉnh nhân vật ở /profile: xem trước, chọn món theo ô (món miễn phí và đã sở hữu), lưu một lần. */
export function AvatarEditor() {
  const mine = useMyAvatar()
  const catalog = useShopItems()

  if (mine.isPending || catalog.isPending) {
    return <p className="text-muted">Đang tải…</p>
  }

  if (mine.isError || catalog.isError) {
    return (
      <ErrorRetry
        message={describeAvatarError(mine.error ?? catalog.error)}
        onRetry={() => {
          void mine.refetch()
          void catalog.refetch()
        }}
      />
    )
  }

  if (catalog.data.length === 0) {
    return <p className="text-muted">Chưa có món nào trong danh mục.</p>
  }

  return <AvatarEditorForm saved={mine.data} items={catalog.data} />
}

function AvatarEditorForm({ saved, items }: { saved: MyAvatar; items: AvatarItem[] }) {
  const save = useSaveAvatar()
  // Chỉ giữ lựa chọn CHƯA LƯU; dữ liệu đã lưu luôn đọc từ server (TanStack Query), không sao chép vào state.
  const [unsaved, setUnsaved] = useState<AvatarData | null>(null)
  const draft = unsaved ?? saved.avatar
  // isPending của TanStack cập nhật chậm một nhịp: bấm đúp thật nhanh vẫn lọt qua, nên chặn đồng bộ bằng ref.
  const sending = useRef(false)

  const dirty = signature(draft) !== signature(saved.avatar)
  const graphicConflict = draft.clothesGraphic !== null && !isPrintable(draft.clothes)

  function submit() {
    if (sending.current) return
    sending.current = true
    save.mutate(draft, {
      onSuccess: () => setUnsaved(null),
      onSettled: () => {
        sending.current = false
      },
    })
  }

  return (
    <section className="card grid gap-5 sm:grid-cols-[auto_1fr]" aria-labelledby="avatar-editor-title">
      <div className="grid justify-items-center content-start gap-3">
        <Avatar avatar={draft} size="lg" label="Xem trước nhân vật" />
        <p className="text-xs text-faint">{dirty ? 'Có thay đổi chưa lưu' : 'Đã lưu'}</p>
      </div>

      <div className="grid gap-4">
        <h2 id="avatar-editor-title">Nhân vật</h2>
        {AVATAR_SLOTS.map((slot) => (
          <AvatarSlotPicker
            key={slot}
            slot={slot}
            options={availableItems(items, saved.ownedItemKeys, slot)}
            value={draft[slot]}
            onChange={(variantKey) => setUnsaved({ ...draft, [slot]: variantKey })}
            lockedReason={slot === 'clothesGraphic' && !isPrintable(draft.clothes) ? GRAPHIC_HINT : null}
          />
        ))}

        {graphicConflict && <p role="alert" className="text-negative">{GRAPHIC_CONFLICT}</p>}
        {save.isError && <p role="alert" className="text-negative">{describeAvatarError(save.error)}</p>}

        <div className="flex flex-wrap gap-3">
          <button
            className="btn btn-primary"
            type="button"
            disabled={!dirty || save.isPending || graphicConflict}
            onClick={submit}
          >
            {save.isPending ? 'Đang lưu…' : 'Lưu nhân vật'}
          </button>
          {dirty && (
            <button className="btn btn-ghost" type="button" disabled={save.isPending} onClick={() => setUnsaved(null)}>
              Bỏ thay đổi
            </button>
          )}
        </div>
      </div>
    </section>
  )
}
