import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { createRoom } from '../../api/rooms'
import { ApiError, describeApiError } from '../../api/apiError'
import { FormField } from '../../components/FormField'
import { hasFormErrors, mapServerErrorsToFields } from '../../validation/authForm'
import { validateCreateRoomForm, type CreateRoomFormErrors } from '../../validation/roomForm'

const CREATE_ROOM_FIELDS = ['topic', 'questionTimeLimitSeconds'] as const

/** Form tạo phòng hiện ngay trên trang (không modal), toggle bằng nút "Tạo phòng" ở DashboardPage. */
export function CreateRoomForm({ onCancel }: { onCancel: () => void }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [topic, setTopic] = useState('')
  const [seconds, setSeconds] = useState(20)
  const [errors, setErrors] = useState<CreateRoomFormErrors>({})
  const [formError, setFormError] = useState<string>()

  const mutation = useMutation({
    mutationFn: () => createRoom({ topic: topic.trim(), questionTimeLimitSeconds: seconds }),
    onSuccess: (room) => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      navigate(`/rooms/${room.code}`)
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError && error.status === 400) {
        setErrors(mapServerErrorsToFields(error.problem?.errors, CREATE_ROOM_FIELDS))
        setFormError(undefined)
      } else {
        setErrors({})
        setFormError(describeApiError(error))
      }
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    const validationErrors = validateCreateRoomForm({ topic, questionTimeLimitSeconds: seconds })
    setErrors(validationErrors)
    setFormError(undefined)
    if (!hasFormErrors(validationErrors)) {
      mutation.mutate()
    }
  }

  return (
    <section className="card col-span-full grid gap-4" aria-labelledby="create-room-title">
      <h2 id="create-room-title">Tạo phòng mới</h2>
      <form className="grid gap-4 min-[561px]:grid-cols-[2fr_1fr_auto] min-[561px]:items-end" onSubmit={handleSubmit} noValidate>
        <FormField id="topic" label="Chủ đề" value={topic} onChange={setTopic} error={errors.topic} />
        <FormField
          id="questionTimeLimitSeconds"
          label="Thời gian mỗi câu (giây)"
          type="number"
          value={String(seconds)}
          onChange={(value) => setSeconds(Number(value))}
          error={errors.questionTimeLimitSeconds}
          hint="5–120 giây."
        />
        <div className="flex gap-2">
          <button className="btn btn-primary" type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Đang tạo…' : 'Tạo phòng'}
          </button>
          <button className="btn btn-ghost" type="button" onClick={onCancel}>
            Hủy
          </button>
        </div>
      </form>
      {formError && (
        <p className="error" role="alert">
          {formError}
        </p>
      )}
    </section>
  )
}
