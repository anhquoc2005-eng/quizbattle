import { describe, expect, it } from 'vitest'
import type { GameEnded, PlayerJoined, PlayerLeft, PlayerReward, QuestionEnded, QuestionStarted, RoomState } from '../types/game'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { gameReducer, initialGameState, type GameState } from './gameReducer'

const ENDED_AT = '2026-01-01T12:00:10.000Z'
const NEXT_AT = '2026-01-01T12:00:16.000Z'

function roomState(overrides: Partial<RoomState> = {}): RoomState {
  return {
    code: 'ABCDEF',
    status: 'InProgress',
    questionTimeLimitSeconds: 20,
    players: [{ playerId: 'p1', displayName: 'Alice', score: 500, level: 2, avatar: TEST_AVATAR }],
    currentQuestion: null,
    leaderboard: [],
    isHost: false,
    playerId: 'p1',
    hasAnsweredCurrentQuestion: null,
    serverTime: new Date().toISOString(),
    rewards: null,
    ...overrides,
  }
}

const REWARD: PlayerReward = {
  playerId: 'p1',
  awarded: true,
  notAwardedReason: null,
  rewardRank: 1,
  score: 900,
  xp: 90,
  coins: 9,
  rankBonus: 30,
  noRankBonusReason: null,
  totalXp: 90,
  levelBefore: 1,
  levelAfter: 1,
  leveledUp: false,
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
      nextQuestionAt: null,
    },
    ...overrides,
  }
}

describe('gameReducer', () => {
  it('ROOM_STATE thay toàn bộ state', () => {
    const dirty: GameState = { ...stateWithQuestion(), players: [{ playerId: 'x', displayName: 'Cũ', score: 1, level: null, online: true }] }

    const next = gameReducer(dirty, { type: 'ROOM_STATE', payload: roomState() })

    expect(next.players).toEqual([{ playerId: 'p1', displayName: 'Alice', score: 500, level: 2, avatar: TEST_AVATAR, online: true }])
    expect(next.currentQuestion).toBeNull()
  })

  it('PLAYER_JOINED với playerId đã có không nhân đôi, chỉ đánh dấu online', () => {
    const state: GameState = {
      ...initialGameState,
      players: [{ playerId: 'p1', displayName: 'Alice', score: 100, level: 2, online: false }],
    }
    const payload: PlayerJoined = { playerId: 'p1', displayName: 'Alice', level: 2, avatar: TEST_AVATAR }

    const next = gameReducer(state, { type: 'PLAYER_JOINED', payload })

    expect(next.players).toHaveLength(1)
    expect(next.players[0]).toEqual({ playerId: 'p1', displayName: 'Alice', score: 100, level: 2, online: true })
  })

  it('PLAYER_JOINED với playerId mới thì thêm vào danh sách', () => {
    const payload: PlayerJoined = { playerId: 'p2', displayName: 'Bob', level: null, avatar: TEST_AVATAR }

    const next = gameReducer(initialGameState, { type: 'PLAYER_JOINED', payload })

    expect(next.players).toEqual([{ playerId: 'p2', displayName: 'Bob', score: 0, level: null, online: true }])
  })

  it('PLAYER_LEFT đánh dấu offline, KHÔNG xóa khỏi danh sách', () => {
    const state: GameState = {
      ...initialGameState,
      players: [{ playerId: 'p1', displayName: 'Alice', score: 100, level: 2, online: true }],
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
    const payload: QuestionEnded = { questionId: 'q1', correctIndex: 2, answeredCount: 3, totalPlayers: 4, endedAt: ENDED_AT, nextQuestionAt: NEXT_AT }

    const next = gameReducer(state, { type: 'QUESTION_ENDED', payload })

    expect(next.currentQuestion?.ended).toBe(true)
    expect(next.currentQuestion?.correctIndex).toBe(2)
    expect(next.currentQuestion?.nextQuestionAt).toBe(NEXT_AT)
    expect(next.answerProgress).toEqual({ answeredCount: 3, totalPlayers: 4 })
  })

  it('QUESTION_ENDED không ghi đè nếu questionId không khớp câu hiện tại (sự kiện đến muộn)', () => {
    const state = stateWithQuestion()
    const payload: QuestionEnded = { questionId: 'câu-khác', correctIndex: 2, answeredCount: 3, totalPlayers: 4, endedAt: ENDED_AT, nextQuestionAt: NEXT_AT }

    const next = gameReducer(state, { type: 'QUESTION_ENDED', payload })

    expect(next.currentQuestion?.correctIndex).toBeNull()
  })

  it('GAME_ENDED lưu bảng xếp hạng cuối và chuyển trạng thái Finished', () => {
    const payload: GameEnded = { finalLeaderboard: [{ playerId: 'p1', displayName: 'Alice', score: 900, rank: 1, level: 2, avatar: TEST_AVATAR }], rewards: [REWARD] }

    const next = gameReducer(stateWithQuestion(), { type: 'GAME_ENDED', payload })

    expect(next.status).toBe('Finished')
    expect(next.finalLeaderboard).toEqual(payload.finalLeaderboard)
    expect(next.leaderboard).toEqual(payload.finalLeaderboard)
  })

  it('GAME_ENDED lưu rewards của từng người chơi', () => {
    const payload: GameEnded = { finalLeaderboard: [], rewards: [REWARD] }

    const next = gameReducer(stateWithQuestion(), { type: 'GAME_ENDED', payload })

    expect(next.rewards).toEqual([REWARD])
  })

  it('ROOM_STATE của phòng đã kết thúc nạp rewards vào state', () => {
    const next = gameReducer(initialGameState, { type: 'ROOM_STATE', payload: roomState({ status: 'Finished', rewards: [REWARD] }) })

    expect(next.rewards).toEqual([REWARD])
  })

  it('ROOM_STATE chưa kết thúc thì rewards là null', () => {
    const next = gameReducer({ ...initialGameState, rewards: [REWARD] }, { type: 'ROOM_STATE', payload: roomState() })

    expect(next.rewards).toBeNull()
  })
})
