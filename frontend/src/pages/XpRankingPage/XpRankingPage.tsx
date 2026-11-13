import { Link } from 'react-router'
import { describeApiError } from '../../api/apiError'
import { Avatar } from '../../components/Avatar'
import { ErrorRetry } from '../../components/ErrorRetry'
import { LogoMark } from '../../components/LogoMark'
import { useXpLeaderboard } from '../../hooks/useXpLeaderboard'

/** Bảng xếp hạng XP công khai (top 50). */
export function XpRankingPage() {
  const query = useXpLeaderboard()

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <Link to="/shop">Cửa hàng</Link>
        </nav>
      </header>

      <main className="col py-6">
        <section className="card" aria-labelledby="ranking-title">
          <h1 id="ranking-title">Xếp hạng XP</h1>
          {query.isPending && <p className="mt-3 text-muted">Đang tải…</p>}
          {query.isError && <ErrorRetry message={describeApiError(query.error)} onRetry={() => void query.refetch()} />}
          {query.isSuccess && query.data.length === 0 && (
            <p className="mt-3 rounded-[14px] border border-dashed border-control p-5 text-muted">Chưa có ai có XP. Hãy thi đấu để lên bảng.</p>
          )}
          {query.isSuccess && query.data.length > 0 && (
            <ol className="mt-3 grid list-none gap-1.5 p-0">
              {query.data.map((entry) => (
                <li key={entry.rank} className="flex items-center justify-between gap-3 rounded-[14px] px-3 py-2">
                  <span className="flex items-center gap-3">
                    <span className="w-8 text-right tabular-nums text-faint">{entry.rank}</span>
                    <Avatar avatar={entry.avatar} size="sm" />
                    <span>{entry.displayName}</span>
                    <span className="text-xs tabular-nums text-muted">Cấp {entry.level}</span>
                  </span>
                  <span className="tabular-nums text-muted">{entry.totalXp} XP</span>
                </li>
              ))}
            </ol>
          )}
        </section>
      </main>
    </>
  )
}
