const TITLE = 'QuizBattle'

/** h1 có tên truy cập đúng một lần (span sr-only); các chữ chỉ để trang trí nên aria-hidden. */
export function HeroTitle() {
  return (
    <h1 className="text-4xl font-bold sm:text-5xl">
      <span className="sr-only">{TITLE}</span>
      {[...TITLE].map((char, i) => (
        <span key={i} aria-hidden="true" className="hero-char" style={{ '--i': i } as React.CSSProperties}>
          {char}
        </span>
      ))}
    </h1>
  )
}
