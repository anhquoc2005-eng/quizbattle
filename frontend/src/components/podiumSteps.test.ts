import { describe, expect, it } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import type { LeaderboardEntry } from '../types/game'
import { podiumSteps } from './podiumSteps'

const entry = (rank: number, score: number, name = `Người ${rank}`): LeaderboardEntry => ({
  playerId: `p${rank}`, displayName: name, score, rank, level: null, avatar: TEST_AVATAR,
})

const ranks = (entries: LeaderboardEntry[]) => podiumSteps(entries).map((s) => s.rank)

describe('podiumSteps', () => {
  it('MoreThanThreePlayers_OnlyTopThreeInRankOrder', () => {
    const steps = podiumSteps([entry(1, 900), entry(2, 700), entry(3, 500), entry(4, 300), entry(5, 100)])

    expect(steps.map((s) => s.rank)).toEqual([1, 2, 3])
    expect(steps.map((s) => s.entry.playerId)).toEqual(['p1', 'p2', 'p3'])
  })

  it('SameScoreButDifferentRank_TwoSeparateSteps_OnePersonEach', () => {
    // Server tie-break theo thời gian trả lời đúng nên rank vẫn duy nhất: không gộp, không bậc nào có hai người.
    const steps = podiumSteps([entry(1, 800, 'Nhanh'), entry(2, 800, 'Chậm'), entry(3, 500)])

    expect(steps.map((s) => [s.rank, s.entry.displayName])).toEqual([[1, 'Nhanh'], [2, 'Chậm'], [3, 'Người 3']])
  })

  it('UnsortedInput_StillOrderedByRank', () => {
    expect(ranks([entry(3, 500), entry(1, 900), entry(2, 700)])).toEqual([1, 2, 3])
  })

  it('OnePlayer_OnlyFirstStep', () => {
    expect(ranks([entry(1, 100)])).toEqual([1])
  })

  it('TwoPlayers_FirstAndSecondOnly_NoEmptyThirdStep', () => {
    expect(ranks([entry(1, 100), entry(2, 50)])).toEqual([1, 2])
  })

  it('Empty_NoSteps', () => {
    expect(podiumSteps([])).toEqual([])
  })

  it('DoesNotMutateTheInput', () => {
    const input = [entry(3, 5), entry(1, 9), entry(2, 7), entry(4, 1)]
    const copy = [...input]

    podiumSteps(input)

    expect(input).toEqual(copy)
  })
})
