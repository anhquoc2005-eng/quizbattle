import { SLOT_LABELS, isRequiredSlot } from '../avatar/avatarSlots'
import type { AvatarItem, AvatarSlot } from '../types/avatar'

interface AvatarSlotPickerProps {
  slot: AvatarSlot
  /** Các món được phép chọn ở ô này (đã lọc: miễn phí và đã sở hữu). */
  options: readonly AvatarItem[]
  /** variantKey đang chọn; null = để trống. */
  value: string | null
  onChange: (variantKey: string | null) => void
  /** Có giá trị thì các món (trừ "Không") bị vô hiệu hóa và hiện lý do. */
  lockedReason?: string | null
}

const CHIP = 'min-h-11 rounded-full border px-3 text-sm sm:min-h-9 disabled:cursor-not-allowed disabled:opacity-40'

/** Một ô của trình chỉnh: nhóm nút chọn món. Ô tùy chọn có thêm nút "Không" để bỏ chọn; ô bắt buộc thì không. */
export function AvatarSlotPicker({ slot, options, value, onChange, lockedReason = null }: AvatarSlotPickerProps) {
  const choose = (selected: boolean) => (selected ? 'border-ink text-ink' : 'border-control text-muted')

  return (
    <fieldset className="grid gap-2" aria-label={SLOT_LABELS[slot]}>
      <legend className="mb-1 text-sm font-medium text-ink">{SLOT_LABELS[slot]}</legend>
      <div className="flex max-h-40 flex-wrap gap-2 overflow-y-auto">
        {!isRequiredSlot(slot) && (
          <button type="button" className={`${CHIP} ${choose(value === null)}`} aria-pressed={value === null} onClick={() => onChange(null)}>
            Không
          </button>
        )}
        {options.map((option) => (
          <button
            key={option.key}
            type="button"
            className={`${CHIP} ${choose(value === option.variantKey)}`}
            aria-pressed={value === option.variantKey}
            disabled={lockedReason !== null}
            onClick={() => onChange(option.variantKey)}
          >
            {option.name}
          </button>
        ))}
      </div>
      {lockedReason !== null && <p className="text-xs text-faint">{lockedReason}</p>}
    </fieldset>
  )
}
