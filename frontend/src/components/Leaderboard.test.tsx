// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { peekAvatarDataUri } from '../avatar/avatarRenderer'
import type { LeaderboardEntry } from '../types/game'
import { Leaderboard } from './Leaderboard'

vi.mock('../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x') }))

const entry = (id: string, name: string, rank: number, score: number): LeaderboardEntry => ({
  playerId: id, displayName: name, score, rank, level: null, avatar: TEST_AVATAR,
})

beforeEach(() => vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue('data:image/svg+xml,x'))
afterEach(cleanup)

describe('Leaderboard', () => {
  it('Empty_ShowsEmptyMessage', () => {
    render(<Leaderboard entries={[]} />)

    expect(screen.getByText('Chưa có ai ghi điểm.')).not.toBeNull()
  })

  it('EveryRowHasASmallAvatarBetweenRankAndName', () => {
    const { container } = render(<Leaderboard entries={[entry('1', 'An', 1, 900), entry('2', 'Bình', 2, 700)]} />)

    const rows = container.querySelectorAll('li')
    expect(rows).toHaveLength(2)
    rows.forEach((row) => {
      const img = row.querySelector('img')!
      expect(img.className).toContain('size-7')
      expect(img.getAttribute('aria-hidden')).toBe('true')
    })
  })

  it('KeepsRankNameAndScore_AndHighlightsSelf', () => {
    const { container } = render(<Leaderboard entries={[entry('1', 'An', 1, 900), entry('2', 'Bình', 2, 700)]} highlightPlayerId="2" />)

    expect(screen.getByText('900')).not.toBeNull()
    expect(screen.getByText('Bình').className).toContain('font-semibold')
    expect(container.querySelectorAll('li')[1]!.className).toContain('bg-raised')
  })
})
