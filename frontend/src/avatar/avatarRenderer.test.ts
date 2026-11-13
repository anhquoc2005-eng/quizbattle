import { describe, expect, it } from 'vitest'
import { TEST_AVATAR } from './avatarFixture'
import { peekAvatarDataUri, renderAvatarDataUri } from './avatarRenderer'

// Chạy DiceBear thật (đúng phiên bản đã ghim) trong môi trường node.
describe('avatarRenderer', () => {
  it('RendersSvgDataUriWithTheRequestedBackground', async () => {
    const uri = await renderAvatarDataUri({ ...TEST_AVATAR, background: 'b9c6d6' })

    expect(uri.startsWith('data:image/svg+xml')).toBe(true)
    expect(decodeURIComponent(uri)).toContain('b9c6d6')
    expect(decodeURIComponent(uri)).not.toContain('<script')
  })

  it('NullBackground_UsesDefaultGrey', async () => {
    const uri = await renderAvatarDataUri({ ...TEST_AVATAR, background: null })

    expect(decodeURIComponent(uri)).toContain('d4d4d4')
  })

  it('SameAvatarIsCached_PeekReturnsItWithoutLoading', async () => {
    const avatar = { ...TEST_AVATAR, hair: 'variant45', background: 'a9d3dc' }
    expect(peekAvatarDataUri(avatar)).toBeUndefined()

    const first = await renderAvatarDataUri(avatar)

    expect(peekAvatarDataUri({ ...avatar })).toBe(first)
    expect(await renderAvatarDataUri({ ...avatar })).toBe(first)
  })

  it('DifferentAvatarsRenderDifferentImages', async () => {
    const a = await renderAvatarDataUri({ ...TEST_AVATAR, hair: 'variant02' })
    const b = await renderAvatarDataUri({ ...TEST_AVATAR, hair: 'variant05' })

    expect(a).not.toBe(b)
  })

  it('EmptyOptionalSlotsDrawLessThanEquippedOnes', async () => {
    const plain = await renderAvatarDataUri(TEST_AVATAR)
    const withGlasses = await renderAvatarDataUri({ ...TEST_AVATAR, glasses: 'variant04' })

    expect(withGlasses.length).toBeGreaterThan(plain.length)
  })
})
