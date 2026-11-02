import { useState } from 'react'
import { Countdown } from '../../components/Countdown'
import { Leaderboard } from '../../components/Leaderboard'
import type { GameState } from '../../realtime/gameReducer'
import { getRemainingMs } from '../../realtime/serverTime'
import { useGameConnection } from '../../realtime/useGameConnection'

export function LiveQuestion({ state }: { state: GameState }) {
  const { endQuestion, nextQuestion } = useGameConnection()
  const [pending, setPending] = useState(false)
  const question = state.currentQuestion

  if (!question) {
    return null
  }

  const timeUp = !question.ended && getRemainingMs(question.deadline, state.clockOffsetMs) <= 0
  const progress = state.answerProgress
  const allAnswered = !!progress && progress.answeredCount >= progress.totalPlayers
  const canEnd = !question.ended && (timeUp || allAnswered)
  const isLast = question.index === question.total - 1

  const handleEnd = async () => {
    setPending(true)
    await endQuestion()
    setPending(false)
  }

  const handleNext = async () => {
    setPending(true)
    await nextQuestion()
    setPending(false)
  }

  return (
    <section className="card grid gap-4">
      <p className="text-xs text-faint">
        Câu {question.index + 1}/{question.total}
      </p>
      <h1>{question.text}</h1>
      {!question.ended && <Countdown deadline={question.deadline} offsetMs={state.clockOffsetMs} totalSeconds={state.questionTimeLimitSeconds} />}

      <ul className="grid list-none gap-2 p-0 sm:grid-cols-2">
        {question.options.map((option, index) => {
          const isCorrect = question.ended && index === question.correctIndex
          return (
            <li key={index} className={`rounded-[14px] border px-4 py-3 ${isCorrect ? 'border-positive' : 'border-control'}`}>
              {option}
              {isCorrect && <span className="ml-2 text-xs font-semibold text-positive">Đáp án đúng</span>}
            </li>
          )
        })}
      </ul>

      {!question.ended && (
        <p className="text-muted">
          Đã trả lời {progress?.answeredCount ?? 0}/{progress?.totalPlayers ?? state.players.length}
        </p>
      )}

      {!question.ended ? (
        <button className="btn btn-primary justify-self-start" type="button" onClick={handleEnd} disabled={!canEnd || pending}>
          {pending ? 'Đang kết thúc…' : 'Kết thúc câu'}
        </button>
      ) : (
        <>
          <div className="grid gap-3 border-t border-rule pt-4">
            <h2>Bảng xếp hạng</h2>
            <Leaderboard entries={state.leaderboard} />
          </div>
          <button className="btn btn-primary justify-self-start" type="button" onClick={handleNext} disabled={pending}>
            {pending ? 'Đang chuyển…' : isLast ? 'Xem kết quả' : 'Câu tiếp theo'}
          </button>
        </>
      )}
    </section>
  )
}
