import { describe, expect, it } from 'vitest'
import type { GameEnded, LeaderboardUpdated, PlayerJoined, PlayerLeft, PlayerReward, QuestionEnded, QuestionStarted, RoomState } from '../types/game'
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
    answeredPlayerIds: null,
    ...overrides,
  }
}

const OTHER_AVATAR = { ...TEST_AVATAR, hair: 'variant05', background: 'b9c6d6' }

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
    const dirty: GameState = { ...stateWithQuestion(), players: [{ playerId: 'x', displayName: 'Cũ', score: 1, level: null, avatar: TEST_AVATAR, online: true }] }

    const next = gameReducer(dirty, { type: 'ROOM_STATE', payload: roomState() })

    expect(next.players).toEqual([{ playerId: 'p1', displayName: 'Alice', score: 500, level: 2, avatar: TEST_AVATAR, online: true }])
    expect(next.currentQuestion).toBeNull()
  })

  it('PLAYER_JOINED với playerId đã có không nhân đôi, đánh dấu online và cập nhật nhân vật mới nhất', () => {
    const state: GameState = {
      ...initialGameState,
      players: [{ playerId: 'p1', displayName: 'Alice', score: 100, level: 2, avatar: OTHER_AVATAR, online: false }],
    }
    const payload: PlayerJoined = { playerId: 'p1', displayName: 'Alice', level: 2, avatar: TEST_AVATAR }

    const next = gameReducer(state, { type: 'PLAYER_JOINED', payload })

    expect(next.players).toHaveLength(1)
    expect(next.players[0]).toEqual({ playerId: 'p1', displayName: 'Alice', score: 100, level: 2, avatar: TEST_AVATAR, online: true })
  })

  it('PLAYER_JOINED với playerId mới thì thêm vào danh sách', () => {
    const payload: PlayerJoined = { playerId: 'p2', displayName: 'Bob', level: null, avatar: TEST_AVATAR }

    const next = gameReducer(initialGameState, { type: 'PLAYER_JOINED', payload })

    expect(next.players).toEqual([{ playerId: 'p2', displayName: 'Bob', score: 0, level: null, avatar: TEST_AVATAR, online: true }])
  })

  it('PLAYER_JOINED mang nhân vật của người mới vào danh sách (khách và tài khoản)', () => {
    const next = gameReducer(initialGameState, { type: 'PLAYER_JOINED', payload: { playerId: 'p3', displayName: 'Khách', level: null, avatar: OTHER_AVATAR } })

    expect(next.players[0]?.avatar).toEqual(OTHER_AVATAR)
  })

  it('PLAYER_LEFT giữ nguyên nhân vật của người đó', () => {
    const state: GameState = { ...initialGameState, players: [{ playerId: 'p1', displayName: 'Alice', score: 0, level: 2, avatar: OTHER_AVATAR, online: true }] }

    const next = gameReducer(state, { type: 'PLAYER_LEFT', payload: { playerId: 'p1' } })

    expect(next.players[0]?.avatar).toEqual(OTHER_AVATAR)
  })

  it('ROOM_STATE giữ nhân vật của players và của leaderboard đúng như server gửi', () => {
    const entry = { playerId: 'p1', displayName: 'Alice', score: 500, rank: 1, level: 2, avatar: OTHER_AVATAR }

    const next = gameReducer(initialGameState, {
      type: 'ROOM_STATE',
      payload: roomState({ players: [{ playerId: 'p1', displayName: 'Alice', score: 500, level: 2, avatar: OTHER_AVATAR }], leaderboard: [entry] }),
    })

    expect(next.players[0]?.avatar).toEqual(OTHER_AVATAR)
    expect(next.leaderboard[0]?.avatar).toEqual(OTHER_AVATAR)
  })

  it('LEADERBOARD_UPDATED giữ nhân vật hiện tại trong từng dòng (không chụp lại cũ)', () => {
    const before = gameReducer(initialGameState, { type: 'ROOM_STATE', payload: roomState({ leaderboard: [{ playerId: 'p1', displayName: 'Alice', score: 1, rank: 1, level: 2, avatar: TEST_AVATAR }] }) })
    const payload: LeaderboardUpdated = { leaderboard: [{ playerId: 'p1', displayName: 'Alice', score: 9, rank: 1, level: 2, avatar: OTHER_AVATAR }] }

    const next = gameReducer(before, { type: 'LEADERBOARD_UPDATED', payload })

    expect(next.leaderboard[0]?.avatar).toEqual(OTHER_AVATAR)
    expect(next.leaderboard[0]?.score).toBe(9)
  })

  it('PLAYER_LEFT đánh dấu offline, KHÔNG xóa khỏi danh sách', () => {
    const state: GameState = {
      ...initialGameState,
      players: [{ playerId: 'p1', displayName: 'Alice', score: 100, level: 2, avatar: TEST_AVATAR, online: true }],
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
    const payload: QuestionEnded = { questionId: 'q1', correctIndex: 2, answeredCount: 3, totalPlayers: 4, endedAt: ENDED_AT, nextQuestionAt: NEXT_AT, correctPlayerIds: [] }

    const next = gameReducer(state, { type: 'QUESTION_ENDED', payload })

    expect(next.currentQuestion?.ended).toBe(true)
    expect(next.currentQuestion?.correctIndex).toBe(2)
    expect(next.currentQuestion?.nextQuestionAt).toBe(NEXT_AT)
    expect(next.answerProgress).toEqual({ answeredCount: 3, totalPlayers: 4 })
  })

  it('QUESTION_ENDED không ghi đè nếu questionId không khớp câu hiện tại (sự kiện đến muộn)', () => {
    const state = stateWithQuestion()
    const payload: QuestionEnded = { questionId: 'câu-khác', correctIndex: 2, answeredCount: 3, totalPlayers: 4, endedAt: ENDED_AT, nextQuestionAt: NEXT_AT, correctPlayerIds: ['p1'] }

    const next = gameReducer(state, { type: 'QUESTION_ENDED', payload })

    expect(next.currentQuestion?.correctIndex).toBeNull()
  })

  it('GAME_ENDED lưu bảng xếp hạng cuối và chuyển trạng thái Finished', () => {
    const payload: GameEnded = { finalLeaderboard: [{ playerId: 'p1', displayName: 'Alice', score: 900, rank: 1, level: 2, avatar: TEST_AVATAR }], rewards: [REWARD] }

    const next = gameReducer(stateWithQuestion(), { type: 'GAME_ENDED', payload })

    expect(next.status).toBe('Finished')
    expect(next.finalLeaderboard).toEqual(payload.finalLeaderboard)
    expect(next.leaderboard).toEqual(payload.finalLeaderboard)
    expect(next.finalLeaderboard?.[0]?.avatar).toEqual(TEST_AVATAR)
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

describe('gameReducer - lớp học (giơ tay, ✓/✗)', () => {
  const progress = (ids: string[]) => ({
    type: 'ANSWER_PROGRESS' as const,
    payload: { answeredCount: ids.length, totalPlayers: 4, answeredPlayerIds: ids },
  })
  const ended = (overrides: Partial<QuestionEnded> = {}) => ({
    type: 'QUESTION_ENDED' as const,
    payload: { questionId: 'q1', correctIndex: 2, answeredCount: 2, totalPlayers: 4, endedAt: ENDED_AT, nextQuestionAt: NEXT_AT, correctPlayerIds: ['a'], ...overrides },
  })

  it('ANSWER_PROGRESS gộp hợp: {A} rồi {B} → {A,B} (hai người nộp đồng thời mỗi gói thiếu người kia)', () => {
    const afterA = gameReducer(stateWithQuestion(), progress(['a']))
    const afterB = gameReducer(afterA, progress(['b']))

    expect(afterB.answeredPlayerIds).toEqual(['a', 'b'])
  })

  it('ANSWER_PROGRESS đến muộn: {A,B} rồi {A} → vẫn {A,B}, không ai hạ tay', () => {
    const both = gameReducer(stateWithQuestion(), progress(['a', 'b']))

    const next = gameReducer(both, progress(['a']))

    expect(next.answeredPlayerIds).toEqual(['a', 'b'])
    expect(next.answeredPlayerIds).toBe(both.answeredPlayerIds) // không ai mới → giữ nguyên tham chiếu
  })

  it('ANSWER_PROGRESS vẫn cập nhật số đếm hiển thị', () => {
    const next = gameReducer(stateWithQuestion(), progress(['a', 'b']))

    expect(next.answerProgress).toEqual({ answeredCount: 2, totalPlayers: 4 })
  })

  it('ANSWER_PROGRESS sau QUESTION_ENDED bị bỏ qua: màn không quay lại pha giơ tay', () => {
    const afterEnd = gameReducer(gameReducer(stateWithQuestion(), progress(['a'])), ended())

    const next = gameReducer(afterEnd, progress(['a', 'b', 'c']))

    expect(next).toBe(afterEnd)
    expect(next.answeredPlayerIds).toEqual(['a'])
  })

  it('ANSWER_PROGRESS khi chưa có câu hiện tại bị bỏ qua', () => {
    const next = gameReducer(initialGameState, progress(['a']))

    expect(next).toBe(initialGameState)
  })

  it('QUESTION_ENDED lưu correctPlayerIds cho đúng câu đang hiển thị', () => {
    const next = gameReducer(stateWithQuestion(), ended({ correctPlayerIds: ['a', 'c'] }))

    expect(next.correctPlayerIds).toEqual(['a', 'c'])
  })

  it('QUESTION_ENDED của câu khác (đến muộn) không ghi đè correctPlayerIds', () => {
    const next = gameReducer(stateWithQuestion(), ended({ questionId: 'câu-khác', correctPlayerIds: ['a'] }))

    expect(next.correctPlayerIds).toBeNull()
  })

  it('QUESTION_STARTED xóa người đã giơ tay và danh sách trả lời đúng của câu trước', () => {
    const state = gameReducer(gameReducer(stateWithQuestion(), progress(['a', 'b'])), ended())
    const payload: QuestionStarted = {
      questionId: 'q2', index: 1, total: 3, text: 'Câu 2?', options: ['A', 'B', 'C', 'D'],
      startedAt: new Date().toISOString(), deadline: new Date(Date.now() + 20_000).toISOString(), serverTime: new Date().toISOString(),
    }

    const next = gameReducer(state, { type: 'QUESTION_STARTED', payload })

    expect(next.answeredPlayerIds).toEqual([])
    expect(next.correctPlayerIds).toBeNull()
  })

  it('ROOM_STATE của chủ phòng vào lại giữa câu THAY THẾ danh sách đã trả lời (không gộp với bản cũ)', () => {
    const stale: GameState = { ...stateWithQuestion(), answeredPlayerIds: ['cũ'], correctPlayerIds: ['cũ'] }

    const next = gameReducer(stale, { type: 'ROOM_STATE', payload: roomState({ isHost: true, playerId: null, answeredPlayerIds: ['a', 'b'] }) })

    expect(next.answeredPlayerIds).toEqual(['a', 'b'])
    expect(next.correctPlayerIds).toBeNull()
  })

  it('ROOM_STATE không có danh sách (người chơi, hoặc câu đã kết thúc) → rỗng', () => {
    const next = gameReducer({ ...stateWithQuestion(), answeredPlayerIds: ['cũ'] }, { type: 'ROOM_STATE', payload: roomState({ answeredPlayerIds: null }) })

    expect(next.answeredPlayerIds).toEqual([])
  })

  it('GAME_ENDED xóa danh sách giơ tay và đúng sai', () => {
    const state = gameReducer(gameReducer(stateWithQuestion(), progress(['a'])), ended())

    const next = gameReducer(state, { type: 'GAME_ENDED', payload: { finalLeaderboard: [], rewards: [] } })

    expect(next.answeredPlayerIds).toEqual([])
    expect(next.correctPlayerIds).toBeNull()
  })

  it('PLAYER_JOINED nối cuối và PLAYER_LEFT giữ người đó tại chỗ (lưới không đổi chỗ)', () => {
    const player = (playerId: string) => ({ playerId, displayName: playerId, score: 0, level: null, avatar: TEST_AVATAR, online: true })
    const state: GameState = { ...initialGameState, players: [player('p1'), player('p2')] }

    const joined = gameReducer(state, { type: 'PLAYER_JOINED', payload: { playerId: 'p3', displayName: 'p3', level: null, avatar: TEST_AVATAR } })
    const left = gameReducer(joined, { type: 'PLAYER_LEFT', payload: { playerId: 'p1' } })

    expect(left.players.map((p) => p.playerId)).toEqual(['p1', 'p2', 'p3'])
    expect(left.players[0]?.online).toBe(false)
  })
})
