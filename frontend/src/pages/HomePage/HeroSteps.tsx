const STEPS = ['Nhập chủ đề', 'AI sinh câu hỏi', 'Thi đấu trực tiếp']

export function HeroSteps() {
  return (
    <ol className="grid gap-3 sm:grid-cols-3">
      {STEPS.map((step, i) => (
        <li
          key={step}
          className="hero-rise rounded-field bg-raised px-4 py-3 text-sm"
          style={{ '--d': `${1.2 + i * 0.25}s` } as React.CSSProperties}
        >
          {step}
        </li>
      ))}
    </ol>
  )
}
