import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError, describeApiError } from '../../api/apiError'
import { generateQuestions } from '../../api/questions'
import { useAiQuota } from '../../hooks/useAiQuota'
import { useQuestions } from '../../hooks/useQuestions'
import { validateGenerateCount } from '../../validation/questionForm'
import { blockToCode, describeAiLimit, getGenerateBlock } from '../../validation/aiQuota'
import { AiQuotaBadge } from './AiQuotaBadge'
import { shouldShowGenerateError, type GenerateError } from './generateError'

/** Câu lỗi cho request generate thất bại: 429 có mã AI_* thì theo mã (hết lượt ngày / dồn dập), còn lại theo trạng thái HTTP. */
function describeGenerateFailure(error: unknown): string {
  const limit = error instanceof ApiError ? describeAiLimit(error.problem?.code, error.problem?.resetAt) : undefined
  return limit ?? describeApiError(error, { 429: 'Bạn đã sinh câu hỏi quá nhiều, thử lại sau.' })
}

/** Khối sinh câu hỏi bằng AI, chỉ hiện khi phòng còn ở Draft (RoomPage kiểm tra trước khi render). */
export function GenerateQuestionsPanel({ code }: { code: string }) {
  const queryClient = useQueryClient()
  const [count, setCount] = useState(5)
  const [countError, setCountError] = useState<string>()
  const [resultMessage, setResultMessage] = useState<string>()
  const [formError, setFormError] = useState<GenerateError>()
  const questionCount = useQuestions(code).data?.length
  const quota = useAiQuota()
  // Nút tắt chỉ để báo nhanh; server vẫn là nguồn sự thật (nhiều tab/thiết bị có thể lệch nhau).
  const block = quota.data ? getGenerateBlock(quota.data) : 'none'

  // Server có thể đã trừ lượt (thành công, hủy) hoặc hoàn lượt (AI lỗi) nên luôn tải lại số lượt, kể cả khi lỗi.
  const refreshQuota = () => queryClient.invalidateQueries({ queryKey: ['ai-quota'] })

  const mutation = useMutation({
    mutationFn: () => generateQuestions(code, count),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['rooms', code, 'questions'] })
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      refreshQuota()
      setFormError(undefined)
      setResultMessage(result.created < result.requested ? `Đã thêm ${result.created}/${result.requested} câu.` : undefined)
    },
    onError: (error: unknown) => {
      refreshQuota()
      setResultMessage(undefined)
      setFormError({ message: describeGenerateFailure(error), atCount: questionCount })
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    const error = validateGenerateCount(count)
    setCountError(error)
    setFormError(undefined)
    setResultMessage(undefined)
    if (!error) {
      mutation.mutate()
    }
  }

  return (
    <section className="card mb-3.5 grid gap-3" aria-labelledby="generate-title">
      <h2 id="generate-title">Sinh câu hỏi bằng AI</h2>
      <AiQuotaBadge query={quota} />
      <form className="flex flex-wrap items-end gap-3" onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="generate-count">Số câu</label>
          <input
            id="generate-count"
            type="number"
            min={5}
            max={20}
            value={count}
            onChange={(event) => setCount(Number(event.target.value))}
          />
        </div>
        <button className="btn btn-primary" type="submit" disabled={mutation.isPending || block !== 'none'}>
          {mutation.isPending ? 'AI đang soạn câu hỏi…' : 'Sinh câu hỏi'}
        </button>
      </form>
      {block !== 'none' && quota.data && (
        <p className="text-negative" role="status">
          {describeAiLimit(blockToCode(block), quota.data.resetAt)} Bạn vẫn thêm câu hỏi thủ công được.
        </p>
      )}
      {countError && <p className="error">{countError}</p>}
      {block === 'none' && shouldShowGenerateError(formError, questionCount) && (
        <p className="error" role="alert">
          {formError?.message}
        </p>
      )}
      {resultMessage && <p className="text-muted">{resultMessage}</p>}
    </section>
  )
}
