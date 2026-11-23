import { describeApiError } from '../../api/apiError'
import { AvatarEditor } from '../../components/AvatarEditor'
import { AppHeader } from '../../components/AppHeader'
import { ErrorRetry } from '../../components/ErrorRetry'
import { useProfile } from '../../hooks/useProfile'
import { ProfileSummary } from './ProfileSummary'
import { RewardHistory } from './RewardHistory'

export function ProfilePage() {
  const query = useProfile()

  return (
    <>
      <AppHeader current="profile" />

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
