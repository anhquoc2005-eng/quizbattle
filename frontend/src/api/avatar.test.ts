import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { getMyAvatar, saveMyAvatar } from './avatar'
import { request } from './client'
import { getShopItems, purchaseItem } from './shop'

vi.mock('./client', () => ({ request: vi.fn().mockResolvedValue(null) }))

beforeEach(() => vi.mocked(request).mockClear())

describe('avatar and shop api (mọi HTTP qua request())', () => {
  it('getShopItems_GetsPublicCatalog', async () => {
    await getShopItems()

    expect(request).toHaveBeenCalledWith('/api/shop/items')
  })

  it('purchaseItem_PostsOnlyTheItemKey_NoPrice', async () => {
    await purchaseItem('hair.variant03')

    expect(request).toHaveBeenCalledWith('/api/shop/purchases', { method: 'POST', body: '{"itemKey":"hair.variant03"}' })
  })

  it('getMyAvatar_GetsOwnAvatar', async () => {
    await getMyAvatar()

    expect(request).toHaveBeenCalledWith('/api/users/me/avatar')
  })

  it('saveMyAvatar_PutsAllElevenSlotsIncludingNulls', async () => {
    await saveMyAvatar(TEST_AVATAR)

    const [path, init] = vi.mocked(request).mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/users/me/avatar')
    expect(init.method).toBe('PUT')
    const body = JSON.parse(init.body as string) as Record<string, unknown>
    expect(Object.keys(body)).toHaveLength(11)
    expect(body.glasses).toBeNull()
    expect(body.hair).toBe('variant01')
  })
})
