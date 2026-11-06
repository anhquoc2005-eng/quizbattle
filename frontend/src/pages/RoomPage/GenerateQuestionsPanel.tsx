import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { describeApiError } from '../../api/apiError'
import { generateQuestions } from '../../api/questions'
import { useQuestions } from '../../hooks/useQuestions'
import { shouldShowGenerateError, type GenerateError } from './generateError'
import { validateGenerateCount } from '../../validation/questionForm'

/** Khối sinh câu hỏi bằng AI, chỉ hiện khi phòng còn ở Draft (RoomPage kiểm tra trước khi render). */
export function GenerateQuestionsPanel({ code }: { code: string }) {
  const queryClient = useQueryClient()
  const [count, setCount] = useState(5)
  const [countError, setCountError] = useState<string>()
  const [resultMessage, setResultMessage] = useState<string>()
  const [formError, setFormError] = useState<GenerateError>()
  const questionCount = useQuestions(code).data?.length

  const mutation = useMutation({
    mutationFn: () => generateQuestions(code, count),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['rooms', code, 'questions'] })
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      setFormError(undefined)
      setResultMessage(result.created < result.requested ? `Đã thêm ${result.created}/${result.requested} câu.` : undefined)
    },
    onError: (error: unknown) => {
      setResultMessage(undefined)
      setFormError({ message: describeApiError(error, { 429: 'Bạn đã sinh câu hỏi quá nhiều, thử lại sau.' }), atCount: questionCount })
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
        <button className="btn btn-primary" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'AI đang soạn câu hỏi…' : 'Sinh câu hỏi'}
        </button>
      </form>
      {countError && <p className="error">{countError}</p>}
      {shouldShowGenerateError(formError, questionCount) && (
        <p className="error" role="alert">
          {formError?.message}
        </p>
      )}
      {resultMessage && <p className="text-muted">{resultMessage}</p>}
    </section>
  )
}
