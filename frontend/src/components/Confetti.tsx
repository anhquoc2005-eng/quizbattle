const PIECE_COUNT = 24

/** Pháo giấy thuần CSS (vị trí, màu, độ trễ theo :nth-child trong index.css): rơi MỘT lần rồi mờ đi, chỉ để trang trí. */
export function Confetti() {
  return (
    <div className="confetti" aria-hidden="true">
      {Array.from({ length: PIECE_COUNT }, (_, i) => (
        <i key={i} />
      ))}
    </div>
  )
}
