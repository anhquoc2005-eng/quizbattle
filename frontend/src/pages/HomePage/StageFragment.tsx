import { QuestionCard } from './QuestionCard'
import { SAMPLE_TOPICS } from './sampleTopics'

const TOPIC = SAMPLE_TOPICS[0]!
const QUESTION = TOPIC.questions[0]!

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between rounded-field bg-raised px-3 py-3">
      <span className="text-muted">{label}</span>
      <span className="font-semibold tabular-nums">{value}</span>
    </div>
  )
}

/** Mảnh giao diện của từng chặng "Cách chơi". Trang trí, nội dung đã có ở câu bên trái nên ẩn với trình đọc màn hình. */
export function StageFragment({ stage }: { stage: number }) {
  if (stage === 0) {
    return (
      <div data-ui aria-hidden="true" className="space-y-3 rounded-card bg-ground p-5 text-base">
        <p className="text-muted">Chủ đề</p>
        <p className="rounded-field bg-raised px-3 py-3 text-lg font-semibold">{TOPIC.name}</p>
        <Row label="Số câu" value="10" />
        <Row label="Thời gian mỗi câu" value="20 giây" />
        <p className="rounded-full bg-ink px-5 py-3 text-center font-semibold text-ground">Soạn câu hỏi</p>
      </div>
    )
  }
  if (stage === 1) {
    return (
      <div data-ui aria-hidden="true" className="space-y-3 rounded-card bg-ground p-5 text-base">
        <p className="text-muted">AI đã soạn {TOPIC.questions.length} câu, chờ bạn duyệt</p>
        {TOPIC.questions.map((q) => (
          <div key={q.text} className="flex items-center justify-between gap-3 rounded-field bg-raised px-3 py-3">
            <span>{q.text}</span>
            <span className="shrink-0 text-sm text-muted">Chờ duyệt</span>
          </div>
        ))}
      </div>
    )
  }
  if (stage === 2) {
    return (
      <div aria-hidden="true" className="space-y-3">
        <QuestionCard question={QUESTION} revealed />
        <div data-ui className="flex flex-wrap items-center gap-3 rounded-card bg-ground p-4 text-base">
          <span className="rounded-full bg-ink px-5 py-2 font-semibold text-ground">Duyệt</span>
          <span className="rounded-full bg-raised px-5 py-2">Sửa</span>
          <span className="rounded-full bg-raised px-5 py-2">Bỏ</span>
          <span className="ml-auto tabular-nums text-accent">2/3 đã duyệt</span>
        </div>
      </div>
    )
  }
  return (
    <div aria-hidden="true">
      <QuestionCard question={QUESTION} timer revealed score="+850" />
    </div>
  )
}
