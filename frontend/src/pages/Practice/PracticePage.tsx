import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError, describeApiError } from '../../api/apiError'
import { ErrorRetry } from '../../components/ErrorRetry'
import { usePracticeSession } from '../../hooks/usePractice'
import { PracticeHeader } from './PracticeHeader'
import { PracticeQuestion, type SubmittedAnswer } from './PracticeQuestion'
import { PracticeSummary } from './PracticeSummary'
import { SESSION_NOT_FOUND_MESSAGE } from './practiceMessages'

export function PracticePage() {
  const { sessionId = '' } = useParams()
  const query = usePracticeSession(sessionId)
  const [submitted, setSubmitted] = useState<SubmittedAnswer | null>(null)
  // Dòng "Câu trước" chỉ cho lần tải đầu (F5 / vào từ danh sách); tắt hẳn sau khi bấm "Câu tiếp".
  const [showPrevious, setShowPrevious] = useState(true)

  function handleNext() {
    setSubmitted(null)
    setShowPrevious(false)
  }

  const session = query.data

  return (
    <>
      <PracticeHeader />
      <main className="col grid gap-3.5 py-6">
        {query.isPending && <p className="text-muted">Đang tải…</p>}
        {query.isError &&
          (query.error instanceof ApiError && query.error.status === 404 ? (
            <div className="grid justify-items-start gap-3">
              <p className="text-negative">{SESSION_NOT_FOUND_MESSAGE}</p>
              <Link className="btn btn-ghost" to="/practice">
                Về danh sách ôn
              </Link>
            </div>
          ) : (
            <ErrorRetry message={describeApiError(query.error)} onRetry={() => void query.refetch()} />
          ))}
        {session &&
          (session.completed && !submitted ? (
            <PracticeSummary session={session} />
          ) : (
            <PracticeQuestion
              session={session}
              submitted={submitted}
              showPrevious={showPrevious}
              onSubmitted={setSubmitted}
              onNext={handleNext}
              refetch={() => query.refetch()}
            />
          ))}
      </main>
    </>
  )
}
