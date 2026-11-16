// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../../avatar/avatarFixture'
import { peekAvatarDataUri } from '../../avatar/avatarRenderer'
import { initialGameState, type GameState } from '../../realtime/gameReducer'
import type { LeaderboardEntry } from '../../types/game'
import { LiveFinished } from './LiveFinished'

vi.mock('../../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x') }))

const entry = (rank: number, name: string, score: number): LeaderboardEntry => ({ playerId: `p${rank}`, displayName: name, score, rank, level: null, avatar: TEST_AVATAR })
const finished = (overrides: Partial<GameState> = {}): GameState => ({
  ...initialGameState, status: 'Finished', isHost: true,
  finalLeaderboard: [entry(1, 'An', 900), entry(2, 'Bình', 700), entry(3, 'Chi', 500)], ...overrides,
})

beforeEach(() => vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue('data:image/svg+xml,x'))
afterEach(cleanup)

const renderScreen = (state: GameState) => render(<MemoryRouter><LiveFinished state={state} /></MemoryRouter>)

describe('LiveFinished', () => {
  it('ShowsPodiumAboveLeaderboardAndKeepsDashboardLink', () => {
    const { container } = renderScreen(finished())

    expect(container.querySelectorAll('li.podium-place')).toHaveLength(3)
    expect(container.querySelectorAll('ol:not(.podium-steps) li')).toHaveLength(3)
    expect(screen.getByRole('link', { name: 'Về Phòng của tôi' })).not.toBeNull()
  })

  it('EmptyRanking_ShowsNoPodiumAndTheEmptyLeaderboardMessage', () => {
    const { container } = renderScreen(finished({ finalLeaderboard: [] }))

    expect(container.querySelector('.podium')).toBeNull()
    expect(screen.getByText('Chưa có ai ghi điểm.')).not.toBeNull()
  })
})
