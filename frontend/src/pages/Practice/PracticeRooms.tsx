import { useState } from 'react'
import { useNavigate } from 'react-router'
import { describeApiError } from '../../api/apiError'
import { AppHeader } from '../../components/AppHeader'
import { ErrorRetry } from '../../components/ErrorRetry'
import { usePracticeRooms, useStartPractice } from '../../hooks/usePractice'
import type { PracticeRoom } from '../../types/practice'

export function PracticeRooms() {
  const query = usePracticeRooms()
  const start = useStartPractice()
  const navigate = useNavigate()
  const [pendingCode, setPendingCode] = useState<string | null>(null)

  async function handleStart(room: PracticeRoom) {
    // Đang có lượt dở thì vào thẳng, không POST.
    if (room.openSessionId) {
      void navigate(`/practice/${room.openSessionId}`)
      return
    }
    setPendingCode(room.roomCode)
    try {
      const ref = await start.mutateAsync(room.roomCode)
      void navigate(`/practice/${ref.sessionId}`)
    } catch {
      // Lỗi hiện qua start.error bên dưới.
    } finally {
      setPendingCode(null)
    }
  }

  const data = query.data
  const percent = data ? Math.min(100, Math.round((data.xpGainedToday / data.dailyXpCap) * 100)) : 0

  return (
    <>
      <AppHeader current="practice" />
      <main className="col grid gap-3.5 py-6">
        <h1>Ôn tập</h1>
        {query.isPending && <p className="text-muted">Đang tải…</p>}
        {query.isError && <ErrorRetry message={describeApiError(query.error)} onRetry={() => void query.refetch()} />}
        {data && (
          <>
            <section className="card grid gap-2" aria-label="XP ôn tập hôm nay">
              <p className="tabular-nums">
                Hôm nay {data.xpGainedToday}/{data.dailyXpCap} XP
              </p>
              <div
                className="h-2 overflow-hidden rounded-full bg-raised"
                role="progressbar"
                aria-label="XP ôn tập hôm nay"
                aria-valuemin={0}
                aria-valuemax={data.dailyXpCap}
                aria-valuenow={data.xpGainedToday}
              >
                <div className="h-full bg-accent" style={{ width: `${percent}%` }} />
              </div>
              <p className="text-xs text-faint">Mỗi câu đúng +{data.xpPerCorrect} XP, tối đa {data.dailyXpCap} XP mỗi ngày.</p>
            </section>

            {start.isError && <p className="text-negative" role="alert">{describeApiError(start.error)}</p>}

            {data.rooms.length === 0 ? (
              <p className="rounded-[14px] border border-dashed border-control p-5 text-muted">
                Chưa có phòng nào để ôn. Chơi hoặc tổ chức một trận, khi trận kết thúc phòng sẽ hiện ở đây.
              </p>
            ) : (
              <ul className="grid gap-3.5">
                {data.rooms.map((room) => (
                  <li key={room.roomCode} className="card flex flex-wrap items-center justify-between gap-3">
                    <div className="grid gap-1">
                      <b className="font-semibold">{room.topic}</b>
                      <span className="text-xs tabular-nums text-faint">
                        <span>{room.roomCode}</span> · {room.questionCount} câu · {room.role === 'host' ? 'Chủ phòng' : 'Người chơi'}
                      </span>
                    </div>
                    <button
                      className="btn btn-ghost"
                      type="button"
                      disabled={pendingCode !== null}
                      aria-label={`${room.openSessionId ? 'Tiếp tục' : 'Ôn'} ${room.topic}`}
                      onClick={() => void handleStart(room)}
                    >
                      {room.openSessionId ? 'Tiếp tục' : 'Ôn'}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </main>
    </>
  )
}
