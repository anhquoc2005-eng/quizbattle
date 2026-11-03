import type { RoomStatus } from './room'

/** Khớp PlayerSummaryDto. */
export interface PlayerSummary {
  playerId: string
  displayName: string
  score: number
}

/** Khớp CurrentQuestionStateDto - correctIndex chỉ có giá trị khi ended = true. */
export interface CurrentQuestionState {
  questionId: string
  index: number
  total: number
  text: string
  options: string[]
  startedAt: string
  deadline: string
  ended: boolean
  correctIndex: number | null
  /** Mốc server tự chuyển câu - null khi câu chưa kết thúc. */
  nextQuestionAt: string | null
}

/** Khớp LeaderboardEntryDto. */
export interface LeaderboardEntry {
  playerId: string
  displayName: string
  score: number
  rank: number
}

/** Khớp RoomStateDto - gửi riêng cho người vừa JoinRoom (lần đầu hoặc vào lại sau khi rớt mạng). */
export interface RoomState {
  code: string
  status: RoomStatus
  questionTimeLimitSeconds: number
  players: PlayerSummary[]
  currentQuestion: CurrentQuestionState | null
  leaderboard: LeaderboardEntry[]
  isHost: boolean
  playerId: string | null
  hasAnsweredCurrentQuestion: boolean | null
  serverTime: string
}

/** Khớp QuestionStartedDto - KHÔNG có correctIndex. */
export interface QuestionStarted {
  questionId: string
  index: number
  total: number
  text: string
  options: string[]
  startedAt: string
  deadline: string
  serverTime: string
}

/** Khớp QuestionEndedDto. */
export interface QuestionEnded {
  questionId: string
  correctIndex: number
  answeredCount: number
  totalPlayers: number
  endedAt: string
  nextQuestionAt: string
}

/** Khớp LeaderboardUpdatedDto. */
export interface LeaderboardUpdated {
  leaderboard: LeaderboardEntry[]
}

/** Khớp GameEndedDto. */
export interface GameEnded {
  finalLeaderboard: LeaderboardEntry[]
}

/** Khớp PlayerJoinedDto. */
export interface PlayerJoined {
  playerId: string
  displayName: string
}

/** Khớp PlayerLeftDto. */
export interface PlayerLeft {
  playerId: string
}

/** Khớp AnswerProgressDto - chỉ chủ phòng nhận. */
export interface AnswerProgress {
  answeredCount: number
  totalPlayers: number
}

/** Khớp AnswerAckDto - kết quả invoke('SubmitAnswer', ...), null khi bị từ chối (xem sự kiện Error). */
export interface AnswerAck {
  received: boolean
}

/** Khớp GameErrorDto - payload sự kiện Error. */
export interface GameError {
  method: string
  code: 'unauthorized' | 'not_found' | 'conflict' | 'unknown'
  message: string
}

/** Khớp CreatePlayerResponse. */
export interface CreatePlayerResult {
  playerId: string
  sessionToken: string
  roomCode: string
}
