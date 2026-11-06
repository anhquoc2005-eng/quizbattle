import { describe, expect, it, vi } from 'vitest'
import type { AuthResponse, AuthUser } from '../types/auth'
import { ApiError } from './apiError'
import { restoreSession } from './restoreSession'

const savedUser: AuthUser = { id: '1', email: 'a@x.com', displayName: 'Đã lưu' }
const serverUser: AuthUser = { id: '1', email: 'a@x.com', displayName: 'Từ server' }
const stored: AuthResponse = { token: 't', expiresAt: '2999-01-01T00:00:00Z', user: savedUser }

describe('restoreSession', () => {
  it('getMe thành công thì trả user từ server, không xóa token', async () => {
    const clear = vi.fn()
    expect(await restoreSession(stored, async () => serverUser, clear)).toEqual(serverUser)
    expect(clear).not.toHaveBeenCalled()
  })

  it('getMe lỗi mạng thì giữ token và trả user đã lưu', async () => {
    const clear = vi.fn()
    const fetchMe = async () => {
      throw new TypeError('Failed to fetch')
    }
    expect(await restoreSession(stored, fetchMe, clear)).toEqual(savedUser)
    expect(clear).not.toHaveBeenCalled()
  })

  it('getMe trả ApiError 500 thì giữ token và trả user đã lưu', async () => {
    const clear = vi.fn()
    const fetchMe = async () => {
      throw new ApiError(500, null)
    }
    expect(await restoreSession(stored, fetchMe, clear)).toEqual(savedUser)
    expect(clear).not.toHaveBeenCalled()
  })

  it('getMe trả 401 thì xóa token và trả null', async () => {
    const clear = vi.fn()
    const fetchMe = async () => {
      throw new ApiError(401, null)
    }
    expect(await restoreSession(stored, fetchMe, clear)).toBeNull()
    expect(clear).toHaveBeenCalledOnce()
  })
})
