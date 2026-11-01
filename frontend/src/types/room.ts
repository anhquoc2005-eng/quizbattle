export type RoomStatus = 'Draft' | 'Lobby' | 'InProgress' | 'Finished'

export const ROOM_STATUS_LABEL: Record<RoomStatus, string> = {
  Draft: 'Soạn câu hỏi',
  Lobby: 'Phòng chờ',
  InProgress: 'Đang chơi',
  Finished: 'Đã kết thúc',
}

/** Khớp với RoomResponse ở QuizBattle.Application/Rooms. */
export interface Room {
  id: string
  code: string
  topic: string
  status: RoomStatus
  questionTimeLimitSeconds: number
  createdAt: string
  questionCount: number
  playerCount: number
}

/** Khớp với CreateRoomRequest ở QuizBattle.Application/Rooms. */
export interface CreateRoomInput {
  topic: string
  questionTimeLimitSeconds: number
}
