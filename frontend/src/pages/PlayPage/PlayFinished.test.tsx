// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../../avatar/avatarFixture'
import { peekAvatarDataUri } from '../../avatar/avatarRenderer'
import { initialGameState, type GameState } from '../../realtime/gameReducer'
import type { LeaderboardEntry, PlayerReward } from '../../types/game'
import { PlayFinished } from './PlayFinished'

vi.mock('../../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x') }))

const entry = (rank: number, name: string, score: number): LeaderboardEntry => ({ playerId: `p${rank}`, displayName: name, score, rank, level: null, avatar: TEST_AVATAR })
const REWARD: PlayerReward = {
  playerId: 'p2', awarded: true, notAwardedReason: null, rewardRank: 2, score: 700, xp: 70, coins: 7, rankBonus: 20, noRankBonusReason: null,
  totalXp: 70, levelBefore: 1, levelAfter: 1, leveledUp: false,
}
const finished = (overrides: Partial<GameState> = {}): GameState => ({
  ...initialGameState, status: 'Finished', playerId: 'p2',
  finalLeaderboard: [entry(1, 'An', 900), entry(2, 'Bình', 700), entry(3, 'Chi', 500)], rewards: [REWARD], ...overrides,
})

beforeEach(() => vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue('data:image/svg+xml,x'))
afterEach(cleanup)

const renderScreen = (state: GameState) => render(<MemoryRouter><PlayFinished state={state} /></MemoryRouter>)

describe('PlayFinished', () => {
  it('ShowsPodiumBeforeRewardAndKeepsLeaderboardAndHomeLink', () => {
    const { container } = renderScreen(finished())

    const podium = container.querySelector('.podium')!
    const reward = screen.getByLabelText('Thưởng của bạn')
    expect(podium.compareDocumentPosition(reward) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(container.querySelectorAll('ol:not(.podium-steps) li')).toHaveLength(3) // bảng xếp hạng vẫn còn
    expect(screen.getByRole('link', { name: 'Về trang chủ' })).not.toBeNull()
  })

  it('FallsBackToLeaderboardWhenFinalLeaderboardMissing', () => {
    const { container } = renderScreen(finished({ finalLeaderboard: null, leaderboard: [entry(1, 'Vào Lại', 100)] }))

    expect(container.querySelector('.podium-name')!.textContent).toBe('Vào Lại')
  })

  it('NoPlayers_NoPodiumButScreenStillRenders', () => {
    const { container } = renderScreen(finished({ finalLeaderboard: [], rewards: null }))

    expect(container.querySelector('.podium')).toBeNull()
    expect(screen.getByText('Trận đã kết thúc')).not.toBeNull()
  })
})
