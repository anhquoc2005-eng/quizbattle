import { describe, expect, it } from 'vitest'
import type { GameEnded, PlayerJoined, PlayerLeft, QuestionEnded, QuestionStarted, RoomState } from '../types/game'
import { gameReducer, initialGameState, type GameState } from './gameReducer'

function roomState(overrides: Partial<RoomState> = {}): RoomState {
  return {
    code: 'ABCDEF',
    status: 'InProgress',
    questionTimeLimitSeconds: 20,
    players: [{ playerId: 'p1', displayName: 'Alice', score: 500 }],
    currentQuestion: null,
    leaderboard: [],
    isHost: false,
    playerId: 'p1',
    hasAnsweredCurrentQuestion: null,
    serverTime: new Date().toISOString(),
    ...overrides,
  }
}

function stateWithQuestion(overrides: Partial<GameState> = {}): GameState {
  return {
    ...initialGameState,
    status: 'InProgress',
    currentQuestion: {
      questionId: 'q1',
      index: 0,
      total: 3,
      text: 'Câu 1?',
      options: ['A', 'B', 'C', 'D'],
      startedAt: new Date().toISOString(),
      deadline: new Date(Date.now() + 20_000).toISOString(),
      ended: false,
      correctIndex: null,
    },
    ...overrides,
  }
}

describe('gameReducer', () => {
  it('ROOM_STATE thay toàn bộ state', () => {
    const dirty: GameState = { ...stateWithQuestion(), players: [{ playerId: 'x', displayName: 'Cũ', score: 1, online: true }] }

    const next = gameReducer(dirty, { type: 'ROOM_STATE', payload: roomState() })

    expect(next.players).toEqual([{ playerId: 'p1', displayName: 'Alice', score: 500, online: true }])
    expect(next.currentQuestion).toBeNull()
  })

  it('PLAYER_JOINED với playerId đã có không nhân đôi, chỉ đánh dấu online', () => {
    const state: GameState = {
      ...initialGameState,
      players: [{ playerId: 'p1', displayName: 'Alice', score: 100, online: false }],
    }
    const payload: PlayerJoined = { playerId: 'p1', displayName: 'Alice' }

    const next = gameReducer(state, { type: 'PLAYER_JOINED', payload })

    expect(next.players).toHaveLength(1)
    expect(next.players[0]).toEqual({ playerId: 'p1', displayName: 'Alice', score: 100, online: true })
  })

  it('PLAYER_JOINED với playerId mới thì thêm vào danh sách', () => {
    const payload: PlayerJoined = { playerId: 'p2', displayName: 'Bob' }

    const next = gameReducer(initialGameState, { type: 'PLAYER_JOINED', payload })

    expect(next.players).toEqual([{ playerId: 'p2', displayName: 'Bob', score: 0, online: true }])
  })

  it('PLAYER_LEFT đánh dấu offline, KHÔNG xóa khỏi danh sách', () => {
    const state: GameState = {
      ...initialGameState,
      players: [{ playerId: 'p1', displayName: 'Alice', score: 100, online: true }],
    }
    const payload: PlayerLeft = { playerId: 'p1' }

    const next = gameReducer(state, { type: 'PLAYER_LEFT', payload })

    expect(next.players).toHaveLength(1)
    expect(next.players[0]?.online).toBe(false)
  })

  it('QUESTION_STARTED đặt câu mới, reset đã trả lời và xóa đáp án đúng cũ', () => {
    const state: GameState = { ...stateWithQuestion(), hasAnswered: true, selectedIndex: 2 }
    state.currentQuestion = { ...state.currentQuestion!, ended: true, correctIndex: 1 }
    const payload: QuestionStarted = {
      questionId: 'q2',
      index: 1,
      total: 3,
      text: 'Câu 2?',
      options: ['A', 'B', 'C', 'D'],
      startedAt: new Date().toISOString(),
      deadline: new Date(Date.now() + 20_000).toISOString(),
      serverTime: new Date().toISOString(),
    }

    const next = gameReducer(state, { type: 'QUESTION_STARTED', payload })

    expect(next.currentQuestion?.questionId).toBe('q2')
    expect(next.currentQuestion?.ended).toBe(false)
    expect(next.currentQuestion?.correctIndex).toBeNull()
    expect(next.hasAnswered).toBe(false)
    expect(next.selectedIndex).toBeNull()
  })

  it('QUESTION_ENDED lưu correctIndex cho đúng câu đang hiển thị', () => {
    const state = stateWithQuestion()
    const payload: QuestionEnded = { questionId: 'q1', correctIndex: 2, answeredCount: 3, totalPlayers: 4 }

    const next = gameReducer(state, { type: 'QUESTION_ENDED', payload })

    expect(next.currentQuestion?.ended).toBe(true)
    expect(next.currentQuestion?.correctIndex).toBe(2)
    expect(next.answerProgress).toEqual({ answeredCount: 3, totalPlayers: 4 })
  })

  it('QUESTION_ENDED không ghi đè nếu questionId không khớp câu hiện tại (sự kiện đến muộn)', () => {
    const state = stateWithQuestion()
    const payload: QuestionEnded = { questionId: 'câu-khác', correctIndex: 2, answeredCount: 3, totalPlayers: 4 }

    const next = gameReducer(state, { type: 'QUESTION_ENDED', payload })

    expect(next.currentQuestion?.correctIndex).toBeNull()
  })

  it('GAME_ENDED lưu bảng xếp hạng cuối và chuyển trạng thái Finished', () => {
    const payload: GameEnded = { finalLeaderboard: [{ playerId: 'p1', displayName: 'Alice', score: 900, rank: 1 }] }

    const next = gameReducer(stateWithQuestion(), { type: 'GAME_ENDED', payload })

    expect(next.status).toBe('Finished')
    expect(next.finalLeaderboard).toEqual(payload.finalLeaderboard)
    expect(next.leaderboard).toEqual(payload.finalLeaderboard)
  })
})
