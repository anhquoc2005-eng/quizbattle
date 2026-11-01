import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { describeApiError } from '../../api/apiError'
import { approveQuestion, deleteQuestion } from '../../api/questions'
import { QUESTION_REVIEW_STATUS_LABEL, type Question } from '../../types/question'
import { QuestionForm } from './QuestionForm'

/** Một câu hỏi: nội dung, 4 đáp án (đáp án đúng đánh dấu bằng chữ + màu, không chỉ dựa màu), Duyệt/Sửa/Xóa. */
export function QuestionCard({ code, question, readOnly }: { code: string; question: Question; readOnly: boolean }) {
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [formError, setFormError] = useState<string>()

  const approveMutation = useMutation({
    mutationFn: () => approveQuestion(code, question.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['rooms', code, 'questions'] }),
    onError: (error: unknown) => setFormError(describeApiError(error)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteQuestion(code, question.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms', code, 'questions'] })
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
    },
    onError: (error: unknown) => setFormError(describeApiError(error)),
  })

  if (editing) {
    return <QuestionForm code={code} mode="edit" question={question} onDone={() => setEditing(false)} />
  }

  return (
    <div className="grid gap-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <p className="font-medium">{question.text}</p>
        <span className="shrink-0 rounded-full bg-raised px-2.5 py-1 text-sm text-muted">
          {QUESTION_REVIEW_STATUS_LABEL[question.reviewStatus]}
        </span>
      </div>

      <ul className="grid gap-1">
        {question.options.map((option, index) => (
          <li key={index} className="flex items-baseline gap-2">
            <span className={index === question.correctIndex ? 'font-semibold' : 'text-muted'}>{option}</span>
            {index === question.correctIndex && <span className="text-sm text-positive">Đáp án đúng</span>}
          </li>
        ))}
      </ul>

      {formError && (
        <p className="error" role="alert">
          {formError}
        </p>
      )}

      {!readOnly && (
        <div className="flex gap-2">
          {question.reviewStatus === 'Pending' && (
            <button className="btn btn-ghost" type="button" onClick={() => approveMutation.mutate()} disabled={approveMutation.isPending}>
              Duyệt
            </button>
          )}
          <button className="btn btn-ghost" type="button" onClick={() => setEditing(true)}>
            Sửa
          </button>
          <button className="btn btn-ghost" type="button" onClick={() => deleteMutation.mutate()} disabled={deleteMutation.isPending}>
            Xóa
          </button>
        </div>
      )}
    </div>
  )
}
