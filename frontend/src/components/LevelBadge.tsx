/** Chữ nhỏ "Cấp N" cạnh tên người chơi. Khách (level = null) không hiện gì. Chỉ dùng chữ, không dựa vào màu. */
export function LevelBadge({ level }: { level: number | null }) {
  if (level === null) {
    return null
  }

  return <span className="text-xs tabular-nums text-muted">Cấp {level}</span>
}
