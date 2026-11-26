const OPTIONS = ['Sydney', 'Canberra', 'Melbourne', 'Perth']

/** Trận mô phỏng chỉ để trang trí: không tương tác, ẩn với trình đọc màn hình, lặp bằng CSS. */
export function HeroDemo() {
  return (
    <div className="card space-y-4 p-5" aria-hidden="true">
      <div className="flex items-center justify-between text-xs text-faint">
        <span>Câu 3/10</span>
        <span className="relative tabular-nums text-accent">
          <span className="hero-score">+850</span>
        </span>
      </div>
      <p className="text-lg font-semibold">Thủ đô của Úc là?</p>
      <div className="h-1.5 overflow-hidden rounded-full bg-raised">
        <div className="hero-timer h-full w-full origin-left bg-accent" />
      </div>
      <ul className="grid grid-cols-2 gap-2">
        {OPTIONS.map((option) => (
          <li key={option} className="relative rounded-field border border-control px-3 py-3 text-sm">
            {option}
            {option === 'Canberra' && <span className="hero-correct" />}
          </li>
        ))}
      </ul>
    </div>
  )
}
