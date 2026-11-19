import type { UseQueryResult } from '@tanstack/react-query'
import { describeApiError } from '../../api/apiError'
import { ErrorRetry } from '../../components/ErrorRetry'
import type { AiQuota } from '../../types/aiQuota'

/** "Còn X/Y lượt AI hôm nay". Lỗi tải số lượt không chặn việc sinh (server mới là nguồn sự thật), chỉ cho thử lại. */
export function AiQuotaBadge({ query }: { query: UseQueryResult<AiQuota> }) {
  if (query.isPending) {
    return <p className="text-muted">Đang tải số lượt AI…</p>
  }

  if (query.isError) {
    return <ErrorRetry message={`Không tải được số lượt AI. ${describeApiError(query.error)}`} onRetry={() => query.refetch()} />
  }

  const { remaining, dailyLimit } = query.data
  return (
    <p className="text-muted">
      Còn <span className="tabular-nums text-accent">{remaining}/{dailyLimit}</span> lượt AI hôm nay
    </p>
  )
}
