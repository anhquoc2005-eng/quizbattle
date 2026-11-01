import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError, describeApiError } from '../../api/apiError'
import { addQuestion, updateQuestion } from '../../api/questions'
import type { Question } from '../../types/question'
import { hasFormErrors, mapServerErrorsToFields } from '../../validation/authForm'
import { validateQuestionForm, type QuestionFormErrors } from '../../validation/questionForm'

const QUESTION_FIELDS = ['text', 'options', 'correctIndex'] as const

interface QuestionFormProps {
  code: string
  mode: 'add' | 'edit'
  question?: Question
  onDone: () => void
}

/** Form thêm câu thủ công và sửa câu dùng chung - mode quyết định gọi addQuestion hay updateQuestion. */
export function QuestionForm({ code, mode, question, onDone }: QuestionFormProps) {
  const queryClient = useQueryClient()
  const [text, setText] = useState(question?.text ?? '')
  const [options, setOptions] = useState<string[]>(question?.options ?? ['', '', '', ''])
  const [correctIndex, setCorrectIndex] = useState<number | null>(question?.correctIndex ?? null)
  const [errors, setErrors] = useState<QuestionFormErrors>({})
  const [formError, setFormError] = useState<string>()

  const mutation = useMutation({
    mutationFn: () => {
      const input = { text: text.trim(), options: options.map((option) => option.trim()), correctIndex: correctIndex! }
      return mode === 'add' ? addQuestion(code, input) : updateQuestion(code, question!.id, input)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms', code, 'questions'] })
      if (mode === 'add') {
        queryClient.invalidateQueries({ queryKey: ['rooms'] })
      }
      onDone()
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError && error.status === 400) {
        setErrors(mapServerErrorsToFields(error.problem?.errors, QUESTION_FIELDS))
        setFormError(undefined)
      } else {
        setErrors({})
        setFormError(describeApiError(error))
      }
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    const validationErrors = validateQuestionForm({ text, options, correctIndex })
    setErrors(validationErrors)
    setFormError(undefined)
    if (!hasFormErrors(validationErrors)) {
      mutation.mutate()
    }
  }

  function handleOptionChange(index: number, value: string) {
    setOptions(options.map((option, i) => (i === index ? value : option)))
  }

  return (
    <form className="grid gap-3 rounded-[14px] bg-raised p-4" onSubmit={handleSubmit} noValidate>
      <div className="field" data-invalid={errors.text ? true : undefined}>
        <label htmlFor="question-text">Nội dung câu hỏi</label>
        <input id="question-text" value={text} onChange={(event) => setText(event.target.value)} />
        {errors.text && <p className="error">{errors.text}</p>}
      </div>

      <fieldset className="grid gap-2 border-0 p-0">
        <legend className="mb-1 font-medium">4 đáp án - chọn đáp án đúng</legend>
        {options.map((option, index) => (
          <div key={index} className="flex items-center gap-3">
            <input
              type="radio"
              name="correctIndex"
              aria-label={`Đáp án đúng là đáp án ${index + 1}`}
              checked={correctIndex === index}
              onChange={() => setCorrectIndex(index)}
              className="size-5 shrink-0"
            />
            <div className="field flex-1">
              <input
                aria-label={`Đáp án ${index + 1}`}
                placeholder={`Đáp án ${index + 1}`}
                value={option}
                onChange={(event) => handleOptionChange(index, event.target.value)}
              />
            </div>
          </div>
        ))}
        {errors.options && <p className="error">{errors.options}</p>}
        {errors.correctIndex && <p className="error">{errors.correctIndex}</p>}
      </fieldset>

      {formError && (
        <p className="error" role="alert">
          {formError}
        </p>
      )}

      <div className="flex gap-2">
        <button className="btn btn-primary" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Đang lưu…' : 'Lưu'}
        </button>
        <button className="btn btn-ghost" type="button" onClick={onDone}>
          Hủy
        </button>
      </div>
    </form>
  )
}
