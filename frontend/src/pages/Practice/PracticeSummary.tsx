import { Link } from 'react-router'
import type { PracticeSession } from '../../types/practice'

export function PracticeSummary({ session }: { session: PracticeSession }) {
  const summary = session.summary
  if (!summary) {
    return null
  }

  return (
    <section className="card grid gap-4" aria-labelledby="practice-summary">
      <h1 id="practice-summary">Hoàn thành lượt ôn</h1>
      <p className="text-muted">{session.topic}</p>
      <dl className="grid grid-cols-2 gap-3">
        <div className="rounded-[14px] bg-raised p-3">
          <dt className="text-xs text-faint">Số câu đúng</dt>
          <dd className="tabular-nums">
            {summary.correctCount} / {session.total}
          </dd>
        </div>
        <div className="rounded-[14px] bg-raised p-3">
          <dt className="text-xs text-faint">XP lượt này</dt>
          <dd className="tabular-nums">+{summary.xpGainedInSession} XP</dd>
        </div>
      </dl>
      <p className="tabular-nums">
        Còn {summary.remainingToday} XP hôm nay
      </p>
      <Link className="btn btn-ghost justify-self-start" to="/practice">
        Về danh sách ôn
      </Link>
    </section>
  )
}
