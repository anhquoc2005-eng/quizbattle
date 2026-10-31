/** 6 ô ký tự mã phòng. variant="slot" = ô trống nét đứt, dùng khi chưa có phòng. */
export function RoomCodeBoxes({
  chars,
  variant = 'filled',
  label,
}: {
  chars: string[]
  variant?: 'filled' | 'slot'
  label?: string
}) {
  return (
    <div className={variant === 'slot' ? 'code slot' : 'code'} aria-label={label} aria-hidden={label ? undefined : true}>
      {chars.map((char, index) => (
        <span key={index}>{char}</span>
      ))}
    </div>
  )
}
