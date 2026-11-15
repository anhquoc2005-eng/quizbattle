import type {
  AnswerProgress,
  CurrentQuestionState,
  GameEnded,
  LeaderboardEntry,
  LeaderboardUpdated,
  PlayerJoined,
  PlayerLeft,
  PlayerReward,
  QuestionEnded,
  QuestionStarted,
  RoomState,
} from '../types/game'
import type { Avatar } from '../types/avatar'
import type { RoomStatus } from '../types/room'
import { computeClockOffsetMs } from './serverTime'

export interface PlayerPresence {
  playerId: string
  displayName: string
  score: number
  level: number | null
  /** Nhân vật hiện tại do server gửi (luôn đủ ô); client không tự sinh. */
  avatar: Avatar
  /** false khi đã nhận PlayerLeft (rớt mạng) - KHÔNG xóa khỏi danh sách, chỉ đánh dấu. */
  online: boolean
}

// State trận lấy từ SignalR (server đẩy xuống), không phải dữ liệu HTTP - không dùng TanStack Query
// (không có gì để "fetch lại", chỉ có sự kiện đẩy tới). Dùng useReducer với hàm thuần dưới đây: reducer
// CHỈ phản chiếu đúng những gì server đã gửi (RoomState/QuestionStarted/...), không tự tính điểm hay suy
// luận thêm - mọi quyết định nghiệp vụ (đúng/sai, điểm, còn hạn hay không) đều do server quyết, client chỉ hiển thị.
export interface GameState {
  code: string | null
  status: RoomStatus | null
  questionTimeLimitSeconds: number
  players: PlayerPresence[]
  currentQuestion: CurrentQuestionState | null
  hasAnswered: boolean
  /** Đáp án chính người chơi này đã chọn cho câu hiện tại - server không gửi lại giá trị này, chỉ nhớ
   * được trong phiên hiện tại (mất nếu tải lại trang giữa câu). Dùng để hiện "Bạn chọn" sau khi câu kết thúc. */
  selectedIndex: number | null
  leaderboard: LeaderboardEntry[]
  isHost: boolean
  playerId: string | null
  answerProgress: AnswerProgress | null
  finalLeaderboard: LeaderboardEntry[] | null
  /** Thưởng của mọi người chơi khi trận kết thúc (server tính); null khi chưa kết thúc. */
  rewards: PlayerReward[] | null
  /** serverTime - Date.now() tại lần nhận gần nhất (RoomState/QuestionStarted) - xem src/realtime/serverTime.ts. */
  clockOffsetMs: number
}

export const initialGameState: GameState = {
  code: null,
  status: null,
  questionTimeLimitSeconds: 0,
  players: [],
  currentQuestion: null,
  hasAnswered: false,
  selectedIndex: null,
  leaderboard: [],
  isHost: false,
  playerId: null,
  answerProgress: null,
  finalLeaderboard: null,
  rewards: null,
  clockOffsetMs: 0,
}

export type GameAction =
  | { type: 'ROOM_STATE'; payload: RoomState }
  | { type: 'PLAYER_JOINED'; payload: PlayerJoined }
  | { type: 'PLAYER_LEFT'; payload: PlayerLeft }
  | { type: 'QUESTION_STARTED'; payload: QuestionStarted }
  | { type: 'QUESTION_ENDED'; payload: QuestionEnded }
  | { type: 'LEADERBOARD_UPDATED'; payload: LeaderboardUpdated }
  | { type: 'ANSWER_PROGRESS'; payload: AnswerProgress }
  | { type: 'GAME_ENDED'; payload: GameEnded }
  // Không phải sự kiện server - dispatch cục bộ ngay sau khi invoke('SubmitAnswer') thành công (ack khác
  // null), để khóa nút ngay lập tức thay vì chờ một broadcast không tồn tại cho riêng người vừa trả lời.
  | { type: 'ANSWER_SUBMITTED'; selectedIndex: number }

export function gameReducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {
    case 'ROOM_STATE': {
      const s = action.payload
      return {
        code: s.code,
        status: s.status,
        questionTimeLimitSeconds: s.questionTimeLimitSeconds,
        players: s.players.map((p) => ({ ...p, online: true })),
        currentQuestion: s.currentQuestion,
        hasAnswered: s.hasAnsweredCurrentQuestion ?? false,
        selectedIndex: null, // server không gửi lại - không khôi phục được sau khi tải lại trang
        leaderboard: s.leaderboard,
        isHost: s.isHost,
        playerId: s.playerId,
        answerProgress: null,
        finalLeaderboard: s.status === 'Finished' ? s.leaderboard : null,
        rewards: s.rewards,
        clockOffsetMs: computeClockOffsetMs(s.serverTime),
      }
    }

    case 'PLAYER_JOINED': {
      const alreadyIn = state.players.some((p) => p.playerId === action.payload.playerId)
      return {
        ...state,
        players: alreadyIn
          ? state.players.map((p) => (p.playerId === action.payload.playerId ? { ...p, online: true, level: action.payload.level, avatar: action.payload.avatar } : p))
          : [...state.players, { playerId: action.payload.playerId, displayName: action.payload.displayName, score: 0, level: action.payload.level, avatar: action.payload.avatar, online: true }],
      }
    }

    case 'PLAYER_LEFT':
      return {
        ...state,
        players: state.players.map((p) => (p.playerId === action.payload.playerId ? { ...p, online: false } : p)),
      }

    case 'QUESTION_STARTED':
      return {
        ...state,
        status: 'InProgress',
        currentQuestion: {
          questionId: action.payload.questionId,
          index: action.payload.index,
          total: action.payload.total,
          text: action.payload.text,
          options: action.payload.options,
          startedAt: action.payload.startedAt,
          deadline: action.payload.deadline,
          ended: false,
          correctIndex: null,
          nextQuestionAt: null,
        },
        hasAnswered: false,
        selectedIndex: null,
        answerProgress: null,
        clockOffsetMs: computeClockOffsetMs(action.payload.serverTime),
      }

    case 'QUESTION_ENDED':
      return {
        ...state,
        currentQuestion:
          state.currentQuestion?.questionId === action.payload.questionId
            ? {
                ...state.currentQuestion,
                ended: true,
                correctIndex: action.payload.correctIndex,
                nextQuestionAt: action.payload.nextQuestionAt,
              }
            : state.currentQuestion,
        answerProgress: { answeredCount: action.payload.answeredCount, totalPlayers: action.payload.totalPlayers },
      }

    case 'LEADERBOARD_UPDATED':
      return { ...state, leaderboard: action.payload.leaderboard }

    case 'ANSWER_PROGRESS':
      return { ...state, answerProgress: action.payload }

    case 'GAME_ENDED':
      return {
        ...state,
        status: 'Finished',
        leaderboard: action.payload.finalLeaderboard,
        finalLeaderboard: action.payload.finalLeaderboard,
        rewards: action.payload.rewards,
      }

    case 'ANSWER_SUBMITTED':
      return { ...state, hasAnswered: true, selectedIndex: action.selectedIndex }

    default:
      return state
  }
}
