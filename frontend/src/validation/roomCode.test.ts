import { describe, expect, it } from 'vitest'
import { isCompleteRoomCode, sanitizeRoomCode } from './roomCode'

describe('sanitizeRoomCode', () => {
  it('SanitizeRoomCode_LowercaseInput_ReturnsUppercase', () => {
    expect(sanitizeRoomCode('a7k29x')).toBe('A7K29X')
  })
  it('SanitizeRoomCode_CharsOutsideAlphabet_AreDropped', () => {
    expect(sanitizeRoomCode('I0O1L-a b')).toBe('AB')
  })
  it('SanitizeRoomCode_LongerThanSix_IsTruncated', () => {
    expect(sanitizeRoomCode('ABCDEFGH')).toBe('ABCDEF')
  })
})

describe('isCompleteRoomCode', () => {
  it('IsCompleteRoomCode_SixValidChars_ReturnsTrue', () => {
    expect(isCompleteRoomCode('A7K29X')).toBe(true)
  })
  it('IsCompleteRoomCode_FiveChars_ReturnsFalse', () => {
    expect(isCompleteRoomCode('A7K29')).toBe(false)
  })
  it('IsCompleteRoomCode_SixCharsWithInvalid_ReturnsFalse', () => {
    expect(isCompleteRoomCode('A7K29O')).toBe(false)
  })
})
