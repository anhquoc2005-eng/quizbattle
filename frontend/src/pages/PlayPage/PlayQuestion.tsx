import { useState } from 'react'
import { Countdown } from '../../components/Countdown'
import { Leaderboard } from '../../components/Leaderboard'
import { NextQuestionIn } from '../../components/NextQuestionIn'
import type { GameState } from '../../realtime/gameReducer'
import { getRemainingMs } from '../../realtime/serverTime'
import { useGameConnection } from '../../realtime/useGameConnection'
import { useTick } from '../../realtime/useTick'

export function PlayQuestion({ state }: { state: GameState }) {
  const { submitAnswer } = useGameConnection()
  const [submitting, setSubmitting] = useState(false)
  const [offlineQuestionId, setOfflineQuestionId] = useState<string | null>(null)
  const question = state.currentQuestion
  // Không có sự kiện server nào báo "đã hết giờ" - phải tự re-render định kỳ để phát hiện qua đồng hồ.
  useTick(!question?.ended)

  if (!question) {
    return null
  }

  // Hết giờ khóa nút ở client để người dùng thấy ngay - server vẫn là nơi quyết định thật (xem SubmitAnswer).
  const timeUp = !question.ended && getRemainingMs(question.deadline, state.clockOffsetMs) <= 0
  const locked = state.hasAnswered || submitting || question.ended || timeUp

  const handleAnswer = async (index: number) => {
    if (locked) return
    setSubmitting(true)
    setOfflineQuestionId(null)
    try {
      await submitAnswer(question.questionId, index)
    } catch {
      // invoke chỉ ném khi kết nối không ở trạng thái Connected (lỗi nghiệp vụ đã về qua sự kiện Error).
      setOfflineQuestionId(question.questionId)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="card grid gap-4">
      <p className="text-xs text-faint">
        Câu {question.index + 1}/{question.total}
      </p>
      <h1>{question.text}</h1>
      {!question.ended && <Countdown deadline={question.deadline} offsetMs={state.clockOffsetMs} totalSeconds={state.questionTimeLimitSeconds} />}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        {question.options.map((option, index) => (
          <AnswerButton
            key={index}
            label={option}
            onClick={() => handleAnswer(index)}
            disabled={locked}
            ended={question.ended}
            isCorrect={question.correctIndex !== null && index === question.correctIndex}
            isSelected={index === state.selectedIndex}
          />
        ))}
      </div>

      {offlineQuestionId === question.questionId && !state.hasAnswered && !question.ended && <p className="error" role="alert">Mất kết nối, thử lại khi đã kết nối lại.</p>}
      {state.hasAnswered && !question.ended && <p className="text-muted">Đã gửi câu trả lời.</p>}
      {!state.hasAnswered && timeUp && !question.ended && <p className="text-muted">Đã hết giờ.</p>}

      {question.ended && (
        <div className="grid gap-3 border-t border-rule pt-4">
          <h2>Bảng xếp hạng</h2>
          <Leaderboard entries={state.leaderboard} highlightPlayerId={state.playerId} />
          {question.nextQuestionAt && (
            <NextQuestionIn
              label={question.index === question.total - 1 ? 'Kết quả cuối sau' : 'Câu tiếp theo sau'}
              nextQuestionAt={question.nextQuestionAt}
              offsetMs={state.clockOffsetMs}
            />
          )}
        </div>
      )}
    </section>
  )
}

function AnswerButton({
  label,
  onClick,
  disabled,
  ended,
  isCorrect,
  isSelected,
}: {
  label: string
  onClick: () => void
  disabled: boolean
  ended: boolean
  isCorrect: boolean
  isSelected: boolean
}) {
  const showCorrect = ended && isCorrect
  const showWrongSelected = ended && isSelected && !isCorrect

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`min-h-14 rounded-[14px] border px-4 py-3 text-left disabled:opacity-60 ${
        showCorrect ? 'border-positive' : showWrongSelected ? 'border-negative' : 'border-control'
      }`}
    >
      <span className="block">{label}</span>
      {showCorrect && <span className="mt-1 block text-xs font-semibold text-positive">Đáp án đúng</span>}
      {showWrongSelected && <span className="mt-1 block text-xs font-semibold text-negative">Bạn chọn</span>}
    </button>
  )
}
