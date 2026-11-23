import { useState } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../../api/apiError'
import { useSubmitPracticeAnswer } from '../../hooks/usePractice'
import type { PracticeAnswerResult, PracticeQuestion as PracticeQuestionData, PracticeSession } from '../../types/practice'
import { PracticeOptions } from './PracticeOptions'
import { describePracticeError, describePreviousResult, describeResult } from './practiceMessages'

/** Câu vừa nộp, giữ ở PracticePage để không mất khi session được tải lại (và để xem được câu cuối trước tổng kết). */
export interface SubmittedAnswer {
  question: PracticeQuestionData
  selectedIndex: number
  result: PracticeAnswerResult
}

interface PracticeQuestionProps {
  session: PracticeSession
  submitted: SubmittedAnswer | null
  showPrevious: boolean
  onSubmitted: (value: SubmittedAnswer) => void
  /** Bấm "Câu tiếp" / "Xem tổng kết": cha bỏ câu vừa nộp và ẩn dòng "Câu trước". */
  onNext: () => void
  refetch: () => Promise<unknown>
}

export function PracticeQuestion({ session, submitted, showPrevious, onSubmitted, onNext, refetch }: PracticeQuestionProps) {
  const [selected, setSelected] = useState<number | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [isAdvancing, setIsAdvancing] = useState(false)
  const submit = useSubmitPracticeAnswer(session.sessionId)

  const question = submitted?.question ?? session.current
  if (!question) {
    return null
  }

  async function handleSubmit() {
    if (selected === null || !question) return
    setError(null)
    try {
      const result = await submit.mutateAsync({ questionId: question.questionId, selectedIndex: selected })
      onSubmitted({ question, selectedIndex: selected, result })
      // Hết lượt: lấy summary từ server (client không tự tính).
      if (result.completed) void refetch()
    } catch (e) {
      setError(e)
      if (e instanceof ApiError && e.status === 409) {
        setSelected(null)
        void refetch()
      }
    }
  }

  async function handleNext() {
    setIsAdvancing(true)
    try {
      // Đã lấy summary lúc nộp câu cuối thì khỏi tải lại.
      if (!(submitted?.result.completed && session.completed)) await refetch()
      setSelected(null)
      setError(null)
      onNext()
    } finally {
      setIsAdvancing(false)
    }
  }

  const isNotFound = error instanceof ApiError && error.status === 404

  return (
    <section className="card grid gap-4" aria-labelledby="practice-question">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <p className="text-xs text-faint">{session.topic}</p>
        <p className="text-xs tabular-nums text-faint">
          Câu {question.index + 1} / {session.total}
        </p>
      </div>

      {showPrevious && !submitted && session.lastResult && <p className="text-muted">{describePreviousResult(session.lastResult)}</p>}

      <h2 id="practice-question">{question.text}</h2>

      <PracticeOptions
        options={question.options}
        selected={submitted ? submitted.selectedIndex : selected}
        disabled={submit.isPending || submitted !== null}
        onSelect={setSelected}
        correctIndex={submitted?.result.correctIndex}
        chosenIndex={submitted?.selectedIndex}
      />

      {error !== null && (
        <p className="text-negative" role="alert">
          {describePracticeError(error)}
          {isNotFound && (
            <>
              {' '}
              <Link className="btn btn-ghost" to="/practice">
                Về danh sách ôn
              </Link>
            </>
          )}
        </p>
      )}

      {submitted ? (
        <div className="grid justify-items-start gap-3">
          <p role="status">{describeResult(submitted.result, submitted.result.remainingToday)}</p>
          <button className="btn btn-primary" type="button" disabled={isAdvancing} onClick={() => void handleNext()}>
            {submitted.result.completed ? 'Xem tổng kết' : 'Câu tiếp'}
          </button>
        </div>
      ) : (
        <button className="btn btn-primary justify-self-start" type="button" disabled={selected === null || submit.isPending} onClick={() => void handleSubmit()}>
          {submit.isPending ? 'Đang gửi…' : 'Nộp đáp án'}
        </button>
      )}
    </section>
  )
}
