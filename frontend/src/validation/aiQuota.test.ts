import { afterEach, describe, expect, it, vi } from 'vitest'
import type { AiQuota } from '../types/aiQuota'
import { blockToCode, describeAiLimit, getGenerateBlock } from './aiQuota'

const quota = (patch: Partial<AiQuota>): AiQuota => ({
  dailyLimit: 20, used: 3, remaining: 17, globalExhausted: false, resetAt: '2026-11-20T00:00:00+07:00', ...patch,
})

describe('getGenerateBlock', () => {
  it('còn lượt và hệ thống còn lượt: không chặn', () => {
    expect(getGenerateBlock(quota({}))).toBe('none')
    expect(getGenerateBlock(quota({ remaining: 1 }))).toBe('none')
  })

  it('hết lượt cá nhân: chặn kiểu user', () => {
    expect(getGenerateBlock(quota({ used: 20, remaining: 0 }))).toBe('user')
  })

  it('hệ thống hết lượt dù cá nhân còn: chặn kiểu global', () => {
    expect(getGenerateBlock(quota({ globalExhausted: true, remaining: 5 }))).toBe('global')
  })

  it('hết cả hai: ưu tiên global (khớp mã server AI_DAILY_LIMIT_GLOBAL)', () => {
    expect(getGenerateBlock(quota({ globalExhausted: true, remaining: 0 }))).toBe('global')
  })
})

describe('blockToCode', () => {
  it('ánh xạ kiểu chặn sang mã lỗi của server', () => {
    expect(blockToCode('user')).toBe('AI_DAILY_LIMIT_USER')
    expect(blockToCode('global')).toBe('AI_DAILY_LIMIT_GLOBAL')
  })
})

describe('describeAiLimit', () => {
  afterEach(() => vi.restoreAllMocks())

  it('AI_DAILY_LIMIT_USER và AI_DAILY_LIMIT_GLOBAL có hai thông báo khác nhau, đều ghi 00:00 giờ Việt Nam', () => {
    const user = describeAiLimit('AI_DAILY_LIMIT_USER', '2026-11-20T00:00:00+07:00')
    const global = describeAiLimit('AI_DAILY_LIMIT_GLOBAL', '2026-11-20T00:00:00+07:00')

    expect(user).toBe('Bạn đã dùng hết lượt AI hôm nay, làm mới lúc 00:00 (giờ Việt Nam).')
    expect(global).toBe('Hệ thống AI hôm nay đã hết lượt, làm mới lúc 00:00 (giờ Việt Nam).')
    expect(user).not.toBe(global)
  })

  it('không có resetAt thì không nhắc giờ làm mới', () => {
    expect(describeAiLimit('AI_DAILY_LIMIT_USER', undefined)).toBe('Bạn đã dùng hết lượt AI hôm nay.')
  })

  it('AI_RATE_LIMIT nói về dồn dập, không nhắc ngày hay giờ reset', () => {
    const message = describeAiLimit('AI_RATE_LIMIT', '2026-11-20T00:00:00+07:00')

    expect(message).toBe('Bạn sinh câu hỏi quá nhanh, thử lại sau ít phút.')
    expect(message).not.toContain('00:00')
  })

  it('mã lạ hoặc thiếu mã trả undefined để dùng câu lỗi chung', () => {
    expect(describeAiLimit(undefined, undefined)).toBeUndefined()
    expect(describeAiLimit('token_expired', '2026-11-20T00:00:00+07:00')).toBeUndefined()
  })

  it('giờ reset cố định "00:00 (giờ Việt Nam)": không phụ thuộc giá trị resetAt và không dùng định dạng theo múi giờ trình duyệt', () => {
    const localeTime = vi.spyOn(Date.prototype, 'toLocaleTimeString')
    const localeString = vi.spyOn(Date.prototype, 'toLocaleString')
    const intl = vi.spyOn(Intl, 'DateTimeFormat')

    // Cùng một mã, resetAt là các chuỗi khác hẳn nhau (kể cả offset khác +07:00) → thông báo phải giống hệt.
    const messages = ['2026-11-20T00:00:00+07:00', '2026-11-19T10:00:00-05:00', '2026-11-19T17:00:00Z', 'không phải ngày'].map((resetAt) =>
      describeAiLimit('AI_DAILY_LIMIT_USER', resetAt),
    )

    expect(new Set(messages).size).toBe(1)
    expect(messages[0]).toContain('00:00 (giờ Việt Nam)')
    expect(localeTime).not.toHaveBeenCalled()
    expect(localeString).not.toHaveBeenCalled()
    expect(intl).not.toHaveBeenCalled()
  })
})
