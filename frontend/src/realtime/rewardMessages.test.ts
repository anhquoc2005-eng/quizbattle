import { describe, expect, it } from 'vitest'
import type { PlayerReward } from '../types/game'
import { describeReward } from './rewardMessages'

function reward(overrides: Partial<PlayerReward> = {}): PlayerReward {
  return {
    playerId: 'p1',
    awarded: true,
    notAwardedReason: null,
    rewardRank: 4,
    score: 800,
    xp: 80,
    coins: 8,
    rankBonus: 0,
    noRankBonusReason: null,
    totalXp: 80,
    levelBefore: 1,
    levelAfter: 1,
    leveledUp: false,
    ...overrides,
  }
}

describe('describeReward', () => {
  it('có thưởng: tóm tắt XP và tổng Xu (phần điểm + thưởng hạng)', () => {
    const d = describeReward(reward({ rewardRank: 1, coins: 8, rankBonus: 30 }))

    expect(d.awarded).toBe(true)
    expect(d.summary).toBe('Bạn nhận 80 XP và 38 Xu')
  })

  it('thưởng bằng 0 vẫn là có thưởng, hiện 0 XP và 0 Xu', () => {
    const d = describeReward(reward({ score: 5, xp: 0, coins: 0, rankBonus: 0 }))

    expect(d.awarded).toBe(true)
    expect(d.summary).toBe('Bạn nhận 0 XP và 0 Xu')
  })

  it('có thưởng hạng thì nêu hạng và số Xu thưởng hạng', () => {
    const d = describeReward(reward({ rewardRank: 2, rankBonus: 20 }))

    expect(d.details).toContain('Hạng thưởng: 2 (+20 Xu)')
  })

  it('không có thưởng hạng và không có lý do thì không nêu gì về thưởng hạng', () => {
    const d = describeReward(reward({ rewardRank: 4, rankBonus: 0, noRankBonusReason: null }))

    expect(d.details.some((line) => line.includes('thưởng hạng'))).toBe(false)
  })

  it('điểm 0 (zero_score) thì nói rõ không có thưởng hạng', () => {
    const d = describeReward(reward({ score: 0, xp: 0, coins: 0, rewardRank: 1, noRankBonusReason: 'zero_score' }))

    expect(d.details).toContain('Điểm 0 nên không có thưởng hạng.')
  })

  it('lên cấp thì có lời chúc kèm cấp mới', () => {
    const d = describeReward(reward({ levelBefore: 1, levelAfter: 2, leveledUp: true, totalXp: 270 }))

    expect(d.levelUpMessage).toBe('Chúc mừng! Bạn đã lên cấp 2.')
  })

  it('không lên cấp thì không có lời chúc', () => {
    expect(describeReward(reward()).levelUpMessage).toBeNull()
  })

  it.each([
    ['guest', 'Khách không nhận thưởng.'],
    ['host', 'Chủ phòng không nhận thưởng trong phòng của mình.'],
    ['no_answers', 'Bạn chưa trả lời câu nào nên không nhận thưởng.'],
    ['not_enough_accounts', 'Trận chưa đủ 2 tài khoản đã tham gia trả lời nên không ai nhận thưởng.'],
  ] as const)('không có thưởng, lý do %s ra đúng câu tiếng Việt', (code, sentence) => {
    const d = describeReward(
      reward({ awarded: false, notAwardedReason: code, xp: 0, coins: 0, rankBonus: 0, totalXp: null, levelBefore: null, levelAfter: null, leveledUp: null }),
    )

    expect(d.awarded).toBe(false)
    expect(d.summary).toBe(sentence)
    expect(d.levelUpMessage).toBeNull()
  })
})
