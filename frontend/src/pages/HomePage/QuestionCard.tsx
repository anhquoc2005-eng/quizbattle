import type { SampleQuestion } from './sampleTopics'

interface Props {
  question: SampleQuestion
  /** Đáp án đúng được tô xanh (chủ phòng đang xem, hoặc đã hết giờ). */
  revealed?: boolean
  /** Thanh giờ chạy bằng CSS (chỉ ở bước thi đấu). */
  timer?: boolean
  score?: string
}

/** Thẻ câu hỏi dựng bằng HTML: mảnh giao diện thật của màn thi đấu, dùng ở trang chủ làm hình minh họa. */
export function QuestionCard({ question, revealed = false, timer = false, score }: Props) {
  return (
    <div data-ui className="space-y-4 rounded-card bg-surface p-5 text-base">
      <div className="flex items-center justify-between text-sm text-muted">
        <span>Câu mẫu</span>
        {score && <span className="home-score tabular-nums text-accent">{score}</span>}
      </div>
      <p className="text-lg font-semibold">{question.text}</p>
      {timer && (
        <div className="h-1.5 overflow-hidden rounded-full bg-raised">
          <div className="home-timer h-full w-full origin-left bg-accent" />
        </div>
      )}
      <ul className="grid gap-2 sm:grid-cols-2">
        {question.options.map((option, i) => {
          const correct = revealed && i === question.correctIndex
          return (
            <li
              key={option}
              className={`rounded-field px-3 py-3 ${correct ? 'bg-positive text-ground font-semibold' : 'bg-raised'}`}
            >
              {option}
            </li>
          )
        })}
      </ul>
    </div>
  )
}
