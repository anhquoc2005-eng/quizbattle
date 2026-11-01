import { useState } from 'react'
import { describeApiError } from '../../api/apiError'
import { ErrorRetry } from '../../components/ErrorRetry'
import { useQuestions } from '../../hooks/useQuestions'
import type { Room } from '../../types/room'
import { QuestionCard } from './QuestionCard'
import { QuestionForm } from './QuestionForm'

const MAX_QUESTIONS_PER_ROOM = 30

/** Danh sách câu hỏi theo thứ tự, tổng số đã duyệt/tối đa, và form thêm câu thủ công (ẩn khi không Draft). */
export function QuestionList({ code, room }: { code: string; room: Room }) {
  const query = useQuestions(code)
  const [adding, setAdding] = useState(false)
  const readOnly = room.status !== 'Draft'

  if (query.isPending) {
    return (
      <section className="card">
        <p className="text-muted">Đang tải câu hỏi…</p>
      </section>
    )
  }

  if (query.isError) {
    return (
      <section className="card">
        <ErrorRetry message={describeApiError(query.error)} onRetry={() => query.refetch()} />
      </section>
    )
  }

  const questions = query.data
  const approvedCount = questions.filter((q) => q.reviewStatus === 'Approved').length

  if (questions.length === 0 && !adding) {
    return (
      <section className="card grid gap-4">
        <p className="text-muted">
          Phòng chưa có câu hỏi nào.{readOnly ? '' : ' Sinh bằng AI ở trên hoặc thêm thủ công.'}
        </p>
        {!readOnly && (
          <button className="btn btn-ghost justify-self-start" type="button" onClick={() => setAdding(true)}>
            Thêm câu thủ công
          </button>
        )}
      </section>
    )
  }

  return (
    <section className="card grid gap-4" aria-labelledby="questions-title">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 id="questions-title">Câu hỏi</h2>
        <span className="text-sm text-muted">
          Đã duyệt {approvedCount}/{questions.length} câu · Tối đa {MAX_QUESTIONS_PER_ROOM} câu
        </span>
      </div>

      <div className="grid gap-6">
        {questions.map((question) => (
          <QuestionCard key={question.id} code={code} question={question} readOnly={readOnly} />
        ))}
      </div>

      {!readOnly &&
        (adding ? (
          <QuestionForm code={code} mode="add" onDone={() => setAdding(false)} />
        ) : (
          <button className="btn btn-ghost justify-self-start" type="button" onClick={() => setAdding(true)}>
            Thêm câu thủ công
          </button>
        ))}
    </section>
  )
}
