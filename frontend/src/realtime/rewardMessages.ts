import type { NotAwardedReason, PlayerReward } from '../types/game'

export interface RewardDescription {
  awarded: boolean
  /** Câu chính: số nhận được, hoặc lý do không nhận thưởng. */
  summary: string
  /** Dòng phụ (hạng thưởng, ghi chú điểm 0). */
  details: string[]
  /** Lời chúc khi lên cấp, null nếu không lên cấp. */
  levelUpMessage: string | null
}

const NOT_AWARDED_MESSAGES: Record<NotAwardedReason, string> = {
  guest: 'Bạn chơi với tư cách khách nên không nhận thưởng.',
  host: 'Bạn là chủ phòng nên không nhận thưởng trong phòng của mình.',
  no_answers: 'Bạn chưa trả lời câu nào nên không nhận thưởng.',
  not_enough_accounts: 'Trận chưa đủ 2 tài khoản đã tham gia trả lời nên không ai nhận thưởng.',
}

/** Chuyển PlayerReward do server tính thành câu hiển thị - hàm thuần, không tự tính thưởng. */
export function describeReward(reward: PlayerReward): RewardDescription {
  if (!reward.awarded) {
    return {
      awarded: false,
      summary: NOT_AWARDED_MESSAGES[reward.notAwardedReason ?? 'not_enough_accounts'],
      details: [],
      levelUpMessage: null,
    }
  }

  const details: string[] = []
  if (reward.rankBonus > 0) {
    details.push(`Hạng thưởng: ${reward.rewardRank} (+${reward.rankBonus} Xu)`)
  }
  if (reward.noRankBonusReason === 'zero_score') {
    details.push('Điểm 0 nên không có thưởng hạng.')
  }

  return {
    awarded: true,
    summary: `Bạn nhận ${reward.xp} XP và ${reward.coins + reward.rankBonus} Xu`,
    details,
    levelUpMessage: reward.leveledUp && reward.levelAfter !== null ? `Chúc mừng! Bạn đã lên cấp ${reward.levelAfter}.` : null,
  }
}
