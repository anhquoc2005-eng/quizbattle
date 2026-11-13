import { Link } from 'react-router'
import { describeApiError } from '../../api/apiError'
import { AvatarEditor } from '../../components/AvatarEditor'
import { ErrorRetry } from '../../components/ErrorRetry'
import { LogoMark } from '../../components/LogoMark'
import { useProfile } from '../../hooks/useProfile'
import { ProfileSummary } from './ProfileSummary'
import { RewardHistory } from './RewardHistory'

export function ProfilePage() {
  const query = useProfile()

  return (
    <>
      <header className="col bar">
        <Link className="mark" to="/">
          <LogoMark />
          QuizBattle
        </Link>
        <nav>
          <Link to="/dashboard">Phòng của tôi</Link>
          <Link to="/shop">Cửa hàng</Link>
          <Link to="/ranking">Xếp hạng</Link>
        </nav>
      </header>

      <main className="col grid gap-3.5 py-6">
        {query.isPending && <p className="text-muted">Đang tải…</p>}
        {query.isError && <ErrorRetry message={describeApiError(query.error)} onRetry={() => void query.refetch()} />}
        {query.isSuccess && (
          <>
            <ProfileSummary profile={query.data} />
            <AvatarEditor />
            <RewardHistory />
          </>
        )}
      </main>
    </>
  )
}
