import { describe, expect, it } from 'vitest'
import { deskStatus, type ClassroomRound } from './deskStatus'

const running = (answered: string[]): ClassroomRound => ({ ended: false, answeredPlayerIds: answered, correctPlayerIds: null })
const finished = (correct: string[] | null, answered: string[] = []): ClassroomRound => ({ ended: true, answeredPlayerIds: answered, correctPlayerIds: correct })

describe('deskStatus', () => {
  it('NoRound_IsIdle', () => {
    expect(deskStatus('a', undefined)).toBe('idle')
  })

  it('Running_AnsweredIsRaised_OthersIdle', () => {
    expect(deskStatus('a', running(['a']))).toBe('raised')
    expect(deskStatus('b', running(['a']))).toBe('idle')
  })

  it('Ended_CorrectIsCorrect_WrongAndNoAnswerAreBothWrong', () => {
    const round = finished(['a'], ['a', 'b']) // b trả lời sai, c không trả lời
    expect(deskStatus('a', round)).toBe('correct')
    expect(deskStatus('b', round)).toBe('wrong')
    expect(deskStatus('c', round)).toBe('wrong')
  })

  it('Ended_RaisedHandsAreReplacedByResult', () => {
    expect(deskStatus('a', finished(['b'], ['a']))).toBe('wrong') // đã giơ tay nhưng sai: hiện ✗, không còn giơ tay
  })

  it('Ended_CorrectListUnknown_StaysIdleInsteadOfGuessing', () => {
    expect(deskStatus('a', finished(null, ['a']))).toBe('idle')
  })
})
