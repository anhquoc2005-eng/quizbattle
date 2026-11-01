import { Link } from 'react-router'
import { ROOM_STATUS_LABEL, type Room } from '../../types/room'

/** Bảng mã/chủ đề/trạng thái/số câu/ngày (≥640px) chuyển sang danh sách 2 dòng mỗi phòng khi màn nhỏ. */
export function RoomList({ rooms, emptyMessage }: { rooms: Room[]; emptyMessage: string }) {
  if (rooms.length === 0) {
    return (
      <div className="mt-4 grid justify-items-start gap-4 rounded-[14px] border border-dashed border-control p-5">
        <p className="text-muted">{emptyMessage}</p>
      </div>
    )
  }

  return (
    <div className="mt-4">
      <div className="hidden sm:grid sm:gap-2">
        <div className="grid grid-cols-[72px_1fr_auto_auto_auto] gap-4 border-b border-rule px-1 pb-2 text-xs text-faint"> {/* taste-skill-ignore: CLAUDE.md quy định border-rule dùng cho đầu bảng lịch sử */}
          <span>Mã</span>
          <span>Chủ đề</span>
          <span>Trạng thái</span>
          <span>Số câu</span>
          <span>Ngày tạo</span>
        </div>
        {rooms.map((room) => (
          <Link
            key={room.id}
            to={`/rooms/${room.code}`}
            className="grid grid-cols-[72px_1fr_auto_auto_auto] items-center gap-4 rounded-[14px] px-1 py-2 hover:bg-raised"
          >
            <span className="font-semibold text-accent tabular-nums">{room.code}</span>
            <span className="truncate">{room.topic}</span>
            <span className="text-muted">{ROOM_STATUS_LABEL[room.status]}</span>
            <span className="tabular-nums text-muted">{room.questionCount}</span>
            <span className="text-faint">{formatDate(room.createdAt)}</span>
          </Link>
        ))}
      </div>

      <div className="grid gap-1 sm:hidden">
        {rooms.map((room) => (
          <Link
            key={room.id}
            to={`/rooms/${room.code}`}
            className="grid gap-0.5 rounded-[14px] px-1 py-2 hover:bg-raised"
          >
            <span className="flex items-baseline gap-2">
              <span className="font-semibold text-accent tabular-nums">{room.code}</span>
              <span className="truncate">{room.topic}</span>
            </span>
            <span className="text-xs text-faint">
              {ROOM_STATUS_LABEL[room.status]} · {room.questionCount} câu · {formatDate(room.createdAt)}
            </span>
          </Link>
        ))}
      </div>
    </div>
  )
}

function formatDate(iso: string): string {
  return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(iso))
}
