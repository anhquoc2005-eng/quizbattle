import { describeApiError } from '../../api/apiError'
import { ErrorRetry } from '../../components/ErrorRetry'
import { useRewards } from '../../hooks/useProfile'

const dateFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' })

/** Lịch sử thưởng với đủ 3 trạng thái: đang tải, lỗi (thử lại), rỗng (chưa có trận được thưởng). */
export function RewardHistory() {
  const query = useRewards()

  return (
    <section className="card" aria-labelledby="rewards-title">
      <h2 id="rewards-title">Lịch sử thưởng</h2>
      {query.isPending && <p className="mt-3 text-muted">Đang tải…</p>}
      {query.isError && <ErrorRetry message={describeApiError(query.error)} onRetry={() => void query.refetch()} />}
      {query.isSuccess && query.data.length === 0 && (
        <p className="mt-3 rounded-[14px] border border-dashed border-control p-5 text-muted">
          Chưa có trận nào được thưởng. Thi đấu cùng ít nhất một tài khoản khác để nhận XP và Xu.
        </p>
      )}
      {query.isSuccess && query.data.length > 0 && (
        <ul className="mt-3 grid list-none gap-2 p-0">
          {query.data.map((item) => (
            <li key={`${item.roomCode}-${item.playedAt}`} className="grid gap-1 rounded-[14px] bg-raised p-3">
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                <span className="font-semibold">{item.topic}</span>
                <span className="text-xs tabular-nums text-faint">{dateFormat.format(new Date(item.playedAt))}</span>
              </div>
              <p className="text-sm tabular-nums text-muted">
                Hạng {item.rewardRank} · {item.score} điểm · +{item.xp} XP · +{item.coins + item.rankBonus} Xu
              </p>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
