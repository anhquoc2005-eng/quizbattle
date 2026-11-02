import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { describeApiError } from '../../api/apiError'
import { openRoom } from '../../api/game'
import { deleteRoom } from '../../api/rooms'
import { RoomCodeBoxes } from '../../components/RoomCodeBoxes'
import { ROOM_STATUS_LABEL, type Room } from '../../types/room'

/** Đầu trang phòng: chủ đề, trạng thái, mã phòng, mở phòng chờ/vào màn điều khiển, xóa phòng. */
export function RoomHeader({ room }: { room: Room }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [formError, setFormError] = useState<string>()
  const [openError, setOpenError] = useState<string>()

  const deleteMutation = useMutation({
    mutationFn: () => deleteRoom(room.code),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      navigate('/dashboard')
    },
    onError: (error: unknown) => setFormError(describeApiError(error)),
  })

  const openMutation = useMutation({
    mutationFn: () => openRoom(room.code),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['rooms', room.code] }),
    onError: (error: unknown) => setOpenError(describeApiError(error)),
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
      {openError && (
        <p className="error" role="alert">
          {openError}
        </p>
      )}

      <div className="flex flex-wrap items-center gap-3">
        {room.status === 'Draft' ? (
          <button className="btn btn-primary" type="button" onClick={() => openMutation.mutate()} disabled={openMutation.isPending}>
            {openMutation.isPending ? 'Đang mở…' : 'Mở phòng chờ'}
          </button>
        ) : (
          <Link className="btn btn-primary" to={`/rooms/${room.code}/live`}>
            Vào màn điều khiển
          </Link>
        )}

        {confirmingDelete ? (
          <>
            <span className="text-muted">Xóa phòng này? Không thể hoàn tác.</span>
            <button className="btn btn-ghost" type="button" onClick={() => setConfirmingDelete(false)}>
              Hủy
            </button>
            <button className="btn btn-primary" type="button" onClick={() => deleteMutation.mutate()} disabled={deleteMutation.isPending}>
              {deleteMutation.isPending ? 'Đang xóa…' : 'Xác nhận xóa'}
            </button>
          </>
        ) : (
          <button className="btn btn-ghost" type="button" onClick={() => setConfirmingDelete(true)}>
            Xóa phòng
          </button>
        )}
      </div>
    </div>
  )
}
