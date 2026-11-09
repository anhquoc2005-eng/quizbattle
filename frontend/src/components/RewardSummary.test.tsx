// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import type { PlayerReward } from '../types/game'
import { RewardSummary } from './RewardSummary'

afterEach(cleanup)

function reward(overrides: Partial<PlayerReward> = {}): PlayerReward {
  return {
    playerId: 'p1',
    awarded: true,
    notAwardedReason: null,
    rewardRank: 2,
    score: 800,
    xp: 80,
    coins: 8,
    rankBonus: 20,
    noRankBonusReason: null,
    totalXp: 80,
    levelBefore: 1,
    levelAfter: 1,
    leveledUp: false,
    ...overrides,
  }
}

describe('RewardSummary', () => {
  it('hiện XP, Xu, hạng thưởng', () => {
    render(<RewardSummary reward={reward()} />)

    expect(screen.getByText('Bạn nhận 80 XP và 28 Xu')).toBeTruthy()
    expect(screen.getByText('Hạng thưởng: 2 (+20 Xu)')).toBeTruthy()
  })

  it('lên cấp thì hiện lời chúc', () => {
    render(<RewardSummary reward={reward({ leveledUp: true, levelBefore: 1, levelAfter: 2 })} />)

    expect(screen.getByText('Chúc mừng! Bạn đã lên cấp 2.')).toBeTruthy()
  })

  it('leveledUp null thì không hiện dòng lên cấp', () => {
    render(<RewardSummary reward={reward({ leveledUp: null, levelBefore: null, levelAfter: null, totalXp: null })} />)

    expect(screen.queryByText(/lên cấp/)).toBeNull()
  })

  it('không có thưởng thì hiện lý do, không hiện số nhận', () => {
    render(<RewardSummary reward={reward({ awarded: false, notAwardedReason: 'not_enough_accounts', xp: 0, coins: 0, rankBonus: 0 })} />)

    expect(screen.getByText('Trận chưa đủ 2 tài khoản đã tham gia trả lời nên không ai nhận thưởng.')).toBeTruthy()
    expect(screen.queryByText(/Bạn nhận/)).toBeNull()
  })

  it('điểm 0 thì ghi chú không có thưởng hạng', () => {
    render(<RewardSummary reward={reward({ score: 0, xp: 0, coins: 0, rankBonus: 0, rewardRank: 1, noRankBonusReason: 'zero_score' })} />)

    expect(screen.getByText('Điểm 0 nên không có thưởng hạng.')).toBeTruthy()
  })

  it('không có dữ liệu thưởng của mình thì không hiện gì', () => {
    const { container } = render(<RewardSummary reward={undefined} />)

    expect(container.firstChild).toBeNull()
  })
})
