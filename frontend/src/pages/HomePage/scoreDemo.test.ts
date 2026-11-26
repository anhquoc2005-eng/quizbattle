import { describe, expect, it } from 'vitest'
import { demoScore } from './scoreDemo'

describe('demoScore', () => {
  it('DemoScore_InstantCorrect_Returns1000', () => expect(demoScore(true, 0)).toBe(1000))
  it('DemoScore_CorrectAtDeadline_Returns500', () => expect(demoScore(true, 20)).toBe(500))
  it('DemoScore_CorrectHalfway_Returns750', () => expect(demoScore(true, 10)).toBe(750))
  it('DemoScore_Wrong_ReturnsZero', () => expect(demoScore(false, 3)).toBe(0))
  it('DemoScore_AfterDeadline_ReturnsZero', () => expect(demoScore(true, 21)).toBe(0))
})
