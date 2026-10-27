import { ApiError } from '../../api/apiError'
import { useHealth } from '../../hooks/useHealth'

/** Thẻ kiểm tra frontend đã nối được backend và database chưa. */
export function BackendStatus() {
  const { data, error, isPending, refetch, isFetching } = useHealth()

  return (
    <section className="rounded-xl border border-line bg-surface-raised p-5">
      <div className="flex items-center justify-between gap-4">
        <h2 className="font-semibold">Trạng thái hệ thống</h2>
        <button
          type="button"
          onClick={() => void refetch()}
          disabled={isFetching}
          className="rounded-lg border border-line px-3 py-1.5 text-sm text-ink-muted transition hover:border-brand-500 hover:text-ink disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isFetching ? 'Đang kiểm tra…' : 'Kiểm tra lại'}
        </button>
      </div>

      <div className="mt-4 text-sm">
        {isPending ? (
          <StatusLine tone="muted" label="Đang kết nối backend…" />
        ) : error ? (
          <StatusLine tone="bad" label={describeError(error)} />
        ) : (
          <ul className="space-y-2">
            <li>
              <StatusLine tone="good" label="Backend: đang chạy" />
            </li>
            <li>
              <StatusLine
                tone={data.database === 'up' ? 'good' : 'warn'}
                label={data.database === 'up' ? 'Database: đã kết nối' : 'Database: chưa kết nối'}
              />
            </li>
          </ul>
        )}
      </div>
    </section>
  )
}

type Tone = 'good' | 'warn' | 'bad' | 'muted'

const DOT_CLASS: Record<Tone, string> = {
  good: 'bg-good',
  warn: 'bg-warn',
  bad: 'bg-bad',
  muted: 'bg-ink-muted animate-pulse',
}

function StatusLine({ tone, label }: { tone: Tone; label: string }) {
  return (
    <span className="flex items-start gap-2">
      <span className={`mt-1.5 size-2.5 shrink-0 rounded-full ${DOT_CLASS[tone]}`} aria-hidden="true" />
      <span>{label}</span>
    </span>
  )
}

function describeError(error: Error): string {
  if (error instanceof ApiError && error.status === 503) {
    return 'Backend đang chạy nhưng không kết nối được database.'
  }
  if (error instanceof ApiError) {
    return error.message
  }
  return 'Không kết nối được backend. Kiểm tra backend đã chạy ở VITE_API_URL chưa.'
}
