import { describe, expect, it } from 'vitest'
import { ApiError } from './apiError'
import { AVATAR_ERROR_MESSAGES, describeAvatarError } from './avatarErrors'

const CODES = [
  'item_not_found',
  'item_not_purchasable',
  'already_owned',
  'insufficient_coins',
  'unknown_variant',
  'item_not_owned',
  'graphic_incompatible_clothes',
  'validation_failed',
]

describe('describeAvatarError', () => {
  it('EveryBackendCodeHasAMessage', () => {
    for (const code of CODES) {
      expect(AVATAR_ERROR_MESSAGES[code], code).toBeTruthy()
    }
    expect(Object.keys(AVATAR_ERROR_MESSAGES).sort()).toEqual([...CODES].sort())
  })

  it('KnownCode_UsesItsMessage_RegardlessOfStatus', () => {
    const error = new ApiError(409, { code: 'insufficient_coins', title: 'Bạn không đủ Xu để mua món này.' })

    expect(describeAvatarError(error)).toBe(AVATAR_ERROR_MESSAGES.insufficient_coins)
  })

  it('UnknownCode_FallsBackToGenericMessage', () => {
    const error = new ApiError(503, { code: 'brand_new_code' })

    expect(describeAvatarError(error)).toBe('Dịch vụ AI đang bận, thử lại sau ít phút.')
  })

  it('NoCode_FallsBackToGenericMessage', () => {
    expect(describeAvatarError(new ApiError(429, null))).toBe('Bạn thao tác quá nhanh, thử lại sau ít phút.')
  })

  it('NonApiError_IsConnectionMessage', () => {
    expect(describeAvatarError(new TypeError('Failed to fetch'))).toBe('Không kết nối được máy chủ.')
  })
})
