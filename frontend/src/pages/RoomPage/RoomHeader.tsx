import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { describeApiError } from '../../api/apiError'
import { deleteRoom } from '../../api/rooms'
import { RoomCodeBoxes } from '../../components/RoomCodeBoxes'
import { ROOM_STATUS_LABEL, type Room } from '../../types/room'

/** Đầu trang phòng: chủ đề, trạng thái, mã phòng và xóa phòng (xác nhận 2 bước tại chỗ, không window.confirm). */
export function RoomHeader({ room }: { room: Room }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [formError, setFormError] = useState<string>()

  const mutation = useMutation({
    mutationFn: () => deleteRoom(room.code),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      navigate('/dashboard')
    },
    onError: (error: unknown) => setFormError(describeApiError(error)),
  })

  return (
    <div className="card mb-3.5 grid gap-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1>{room.topic}</h1>
          <p className="text-muted">{ROOM_STATUS_LABEL[room.status]}</p>
        </div>
        <RoomCodeBoxes chars={room.code.split('')} label={`Mã phòng ${room.code}`} />
      </div>

      {formError && (
        <p className="error" role="alert">
          {formError}
        </p>
      )}

      {confirmingDelete ? (
        <div className="flex flex-wrap items-center gap-3">
          <span className="text-muted">Xóa phòng này? Không thể hoàn tác.</span>
          <button className="btn btn-ghost" type="button" onClick={() => setConfirmingDelete(false)}>
            Hủy
          </button>
          <button className="btn btn-primary" type="button" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending ? 'Đang xóa…' : 'Xác nhận xóa'}
          </button>
        </div>
      ) : (
        <button className="btn btn-ghost justify-self-start" type="button" onClick={() => setConfirmingDelete(true)}>
          Xóa phòng
        </button>
      )}
    </div>
  )
}
