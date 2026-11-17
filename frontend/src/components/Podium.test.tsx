// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { peekAvatarDataUri } from '../avatar/avatarRenderer'
import type { LeaderboardEntry } from '../types/game'
import { Podium } from './Podium'

vi.mock('../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x') }))

const entry = (rank: number, name: string, score: number): LeaderboardEntry => ({
  playerId: `p${rank}`, displayName: name, score, rank, level: null, avatar: TEST_AVATAR,
})

beforeEach(() => vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue('data:image/svg+xml,x'))
afterEach(cleanup)

const places = (container: HTMLElement) => Array.from(container.querySelectorAll('li.podium-place'))

describe('Podium', () => {
  it('ThreeOrMorePlayers_ShowsTopThreeWithRightPeopleAndScores', () => {
    const { container } = render(<Podium entries={[entry(1, 'An', 900), entry(2, 'Bình', 700), entry(3, 'Chi', 500), entry(4, 'Dũng', 300)]} />)

    expect(places(container).map((p) => p.getAttribute('data-rank'))).toEqual(['1', '2', '3'])
    expect(places(container).map((p) => p.querySelector('.podium-name')!.textContent)).toEqual(['An', 'Bình', 'Chi'])
    expect(places(container).map((p) => p.querySelector('.podium-score')!.textContent)).toEqual(['900', '700', '500'])
    expect(screen.queryByText('Dũng')).toBeNull() // hạng 4 không lên bục
  })

  it('DomIsInRankOrder_ForScreenReaders_EachStepSaysItsRank', () => {
    const { container } = render(<Podium entries={[entry(3, 'Chi', 500), entry(1, 'An', 900), entry(2, 'Bình', 700)]} />)

    expect(places(container).map((p) => p.querySelector('.sr-only')!.textContent)).toEqual(['Hạng 1: ', 'Hạng 2: ', 'Hạng 3: '])
  })

  it('EveryStepHasItsAvatarAndBlock', () => {
    const { container } = render(<Podium entries={[entry(1, 'An', 900), entry(2, 'Bình', 700), entry(3, 'Chi', 500)]} />)

    places(container).forEach((p) => {
      expect(p.querySelectorAll('img')).toHaveLength(1)
      expect(p.querySelector('.podium-block')).not.toBeNull()
    })
  })

  it('TwoPlayers_OnlyTwoSteps_NoEmptyThird', () => {
    const { container } = render(<Podium entries={[entry(1, 'An', 900), entry(2, 'Bình', 700)]} />)

    expect(places(container).map((p) => p.getAttribute('data-rank'))).toEqual(['1', '2'])
  })

  it('OnePlayer_OnlyFirstStep', () => {
    const { container } = render(<Podium entries={[entry(1, 'An', 900)]} />)

    expect(places(container)).toHaveLength(1)
  })

  it('SameScoreDifferentRank_TwoDifferentSteps', () => {
    const { container } = render(<Podium entries={[entry(1, 'Nhanh', 800), entry(2, 'Chậm', 800)]} />)

    expect(places(container).map((p) => p.querySelector('.podium-name')!.textContent)).toEqual(['Nhanh', 'Chậm'])
    expect(places(container).every((p) => p.querySelectorAll('.podium-name').length === 1)).toBe(true) // mỗi bậc một người
  })

  it('NoPlayers_RendersNothing', () => {
    const { container } = render(<Podium entries={[]} />)

    expect(container.firstChild).toBeNull()
  })

  it('Confetti_IsDecorative_HiddenFromScreenReaders', () => {
    const { container } = render(<Podium entries={[entry(1, 'An', 900)]} />)

    const confetti = container.querySelector('.confetti')!
    expect(confetti.getAttribute('aria-hidden')).toBe('true')
    expect(confetti.querySelectorAll('i')).toHaveLength(24)
  })

  it('Avatar_FillsItsColumn_SoItShrinksOnPhones', () => {
    const { container } = render(<Podium entries={[entry(1, 'An', 900)]} />)

    expect(container.querySelector('.podium-avatar img')!.className).toContain('w-full')
  })
})
