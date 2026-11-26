interface Props {
  id?: string
  min: number
  max: number
  value: number
  onChange: (value: number) => void
  ariaLabel?: string
  valueText?: string
}

/** Thanh trượt thật (input range) có rãnh xám và phần đã đi qua tô vàng (là dữ liệu: tiến độ). Thumb trắng 32px, dùng được bằng phím mũi tên. */
export function StepRange({ id, min, max, value, onChange, ariaLabel, valueText }: Props) {
  const percent = max === min ? 0 : ((value - min) / (max - min)) * 100
  return (
    <div className="relative h-8">
      <div className="absolute inset-x-0 top-3 h-2 rounded-full bg-raised" />
      <div className="absolute left-0 top-3 h-2 rounded-full bg-accent" style={{ width: `${percent}%` }} />
      <input
        id={id}
        type="range"
        min={min}
        max={max}
        step={1}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        aria-label={ariaLabel}
        aria-valuetext={valueText}
        className="home-range absolute inset-0"
      />
    </div>
  )
}
