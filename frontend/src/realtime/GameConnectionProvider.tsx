import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { createContext, useEffect, useMemo, useReducer, useRef, useState, type ReactNode } from 'react'
import { loadAuth } from '../api/tokenStorage'
import type { AnswerAck, AnswerProgress, GameEnded, GameError, LeaderboardUpdated, PlayerJoined, PlayerLeft, QuestionEnded, QuestionStarted, RoomState } from '../types/game'
import { GAME_HUB_EVENTS, GAME_HUB_METHODS } from './events'
import { gameReducer, initialGameState, type GameState } from './gameReducer'

const API_URL = import.meta.env.VITE_API_URL

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

export interface GameConnectionContextValue {
  state: GameState
  connectionStatus: ConnectionStatus
  lastError: GameError | null
  clearError: () => void
  startGame: () => Promise<void>
  endQuestion: () => Promise<void>
  nextQuestion: () => Promise<void>
  submitAnswer: (questionId: string, selectedIndex: number) => Promise<AnswerAck | null>
  /** Gọi khi withAutomaticReconnect() đã hết số lần thử (connectionStatus = 'disconnected') hoặc JoinRoom lỗi - tạo lại kết nối từ đầu. */
  reconnect: () => void
}

export const GameConnectionContext = createContext<GameConnectionContextValue | null>(null)

/**
 * MỘT HubConnection duy nhất cho cả màn chơi - không tạo kết nối trong từng component. Provider này chỉ
 * bọc bên trong /play/:code và /rooms/:code/live (không đặt ở main.tsx như AuthProvider): tạo kết nối khi
 * vào màn chơi, dừng hẳn khi rời màn chơi (cleanup của effect dưới đây).
 */
export function GameConnectionProvider({
  code,
  sessionToken,
  children,
}: {
  code: string
  /** Bỏ trống cho chủ phòng - provider tự đọc JWT từ tokenStorage. */
  sessionToken?: string
  children: ReactNode
}) {
  const [state, dispatch] = useReducer(gameReducer, initialGameState)
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('connecting')
  const [lastError, setLastError] = useState<GameError | null>(null)
  const connectionRef = useRef<HubConnection | null>(null)
  const [retryTick, setRetryTick] = useState(0)

  useEffect(() => {
    // StrictMode (dev) chạy effect 2 lần: mount -> cleanup ngay -> mount lại. "stopped" đánh dấu cleanup
    // của LẦN CHẠY NÀY đã xảy ra, để phân biệt với lỗi kết nối thật.
    let stopped = false

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/game`, { accessTokenFactory: () => loadAuth()?.token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on(GAME_HUB_EVENTS.ROOM_STATE, (payload: RoomState) => dispatch({ type: 'ROOM_STATE', payload }))
    connection.on(GAME_HUB_EVENTS.PLAYER_JOINED, (payload: PlayerJoined) => dispatch({ type: 'PLAYER_JOINED', payload }))
    connection.on(GAME_HUB_EVENTS.PLAYER_LEFT, (payload: PlayerLeft) => dispatch({ type: 'PLAYER_LEFT', payload }))
    connection.on(GAME_HUB_EVENTS.QUESTION_STARTED, (payload: QuestionStarted) => dispatch({ type: 'QUESTION_STARTED', payload }))
    connection.on(GAME_HUB_EVENTS.QUESTION_ENDED, (payload: QuestionEnded) => dispatch({ type: 'QUESTION_ENDED', payload }))
    connection.on(GAME_HUB_EVENTS.LEADERBOARD_UPDATED, (payload: LeaderboardUpdated) => dispatch({ type: 'LEADERBOARD_UPDATED', payload }))
    connection.on(GAME_HUB_EVENTS.ANSWER_PROGRESS, (payload: AnswerProgress) => dispatch({ type: 'ANSWER_PROGRESS', payload }))
    connection.on(GAME_HUB_EVENTS.GAME_ENDED, (payload: GameEnded) => dispatch({ type: 'GAME_ENDED', payload }))
    connection.on(GAME_HUB_EVENTS.ERROR, (payload: GameError) => setLastError(payload))

    connection.onreconnecting(() => setConnectionStatus('reconnecting'))
    connection.onreconnected(() => {
      setConnectionStatus('connected')
      // connectionId mới sau khi kết nối lại không còn trong group trên server - phải JoinRoom lại để
      // server thêm vào group và gửi RoomState đồng bộ lại, nếu không sẽ không nhận broadcast nữa.
      void connection.invoke(GAME_HUB_METHODS.JOIN_ROOM, code, sessionToken ?? null)
    })
    connection.onclose(() => {
      if (!stopped) setConnectionStatus('disconnected')
    })

    connectionRef.current = connection

    connection
      .start()
      .then(() => {
        if (stopped) return
        setConnectionStatus('connected')
        return connection.invoke(GAME_HUB_METHODS.JOIN_ROOM, code, sessionToken ?? null)
      })
      .catch(() => {
        // Nếu start() còn đang negotiate lúc bị stop() hủy (xem cleanup bên dưới), promise reject với lỗi
        // "stopped during negotiation" - không phải lỗi kết nối thật, bỏ qua khi đã chủ động dừng.
        if (stopped) return
        setConnectionStatus('disconnected')
      })

    return () => {
      stopped = true
      void connection.stop()
    }
  }, [code, sessionToken, retryTick])

  const value = useMemo<GameConnectionContextValue>(
    () => ({
      state,
      connectionStatus,
      lastError,
      clearError: () => setLastError(null),
      startGame: async () => {
        await connectionRef.current?.invoke(GAME_HUB_METHODS.START_GAME)
      },
      endQuestion: async () => {
        await connectionRef.current?.invoke(GAME_HUB_METHODS.END_QUESTION)
      },
      nextQuestion: async () => {
        await connectionRef.current?.invoke(GAME_HUB_METHODS.NEXT_QUESTION)
      },
      submitAnswer: async (questionId, selectedIndex) => {
        const ack = (await connectionRef.current?.invoke<AnswerAck | null>(GAME_HUB_METHODS.SUBMIT_ANSWER, questionId, selectedIndex)) ?? null
        // ack = null nghĩa là filter backend đã bắt lỗi (xem sự kiện Error riêng) - không đánh dấu đã trả lời.
        if (ack) {
          dispatch({ type: 'ANSWER_SUBMITTED', selectedIndex })
        }
        return ack
      },
      reconnect: () => {
        // Xóa lỗi cũ để màn quay về "Đang kết nối…" thay vì hiện lại lỗi JoinRoom của lần trước.
        setLastError(null)
        setConnectionStatus('connecting')
        setRetryTick((tick) => tick + 1)
      },
    }),
    [state, connectionStatus, lastError],
  )

  return <GameConnectionContext.Provider value={value}>{children}</GameConnectionContext.Provider>
}
