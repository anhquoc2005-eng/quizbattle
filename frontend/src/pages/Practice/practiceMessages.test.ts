import { describe, expect, it } from 'vitest'
import { describePreviousResult, describeResult } from './practiceMessages'

const base = { isCorrect: true, correctIndex: 2, xpAwarded: 5, xpReason: null } as const

describe('practiceMessages', () => {
  it('DescribeResult_FullXp_ShowsXpAndRemaining', () => {
    expect(describeResult(base, 60)).toBe('Đúng, +5 XP, còn 60 hôm nay')
  })

  it('DescribeResult_Wrong_ShowsCorrectOptionNumber', () => {
    expect(describeResult({ ...base, isCorrect: false, xpAwarded: 0, xpReason: 'wrong' }, 60)).toBe('Sai, đáp án đúng là lựa chọn 3')
  })

  it('DescribeResult_AlreadyToday_SaysNoXp', () => {
    expect(describeResult({ ...base, xpAwarded: 0, xpReason: 'already_today' })).toBe('Đúng, câu này đã nhận XP hôm nay')
  })

  it('DescribeResult_DailyCap_SaysCapReached', () => {
    expect(describeResult({ ...base, xpAwarded: 0, xpReason: 'daily_cap' })).toBe('Đúng, hôm nay đã đạt trần XP')
  })

  it('DescribeResult_DailyCapPartial_ShowsOddXp', () => {
    expect(describeResult({ ...base, xpAwarded: 3, xpReason: 'daily_cap_partial' }, 0)).toBe('Đúng, +3 XP, chạm trần ngày, còn 0 hôm nay')
  })

  it('DescribePreviousResult_PrefixesAndOmitsRemaining', () => {
    expect(describePreviousResult(base)).toBe('Câu trước: Đúng, +5 XP')
    expect(describePreviousResult({ ...base, isCorrect: false, xpAwarded: 0, xpReason: 'wrong' })).toBe('Câu trước: Sai, đáp án đúng là lựa chọn 3')
  })
})
