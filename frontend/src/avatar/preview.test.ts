import { describe, expect, it } from 'vitest'
import type { AvatarItem } from '../types/avatar'
import { TEST_AVATAR } from './avatarFixture'
import { GUEST_PREVIEW_AVATAR } from './guestPreview'
import { previewWith } from './preview'

const item = (slot: AvatarItem['slot'], variantKey: string): AvatarItem => ({
  key: `${slot}.${variantKey}`, slot, variantKey, name: variantKey, tier: 'common', price: 50,
})

describe('previewWith', () => {
  it('PutsTheItemIntoItsSlot_LeavingTheRestUntouched', () => {
    const { avatar, note } = previewWith(TEST_AVATAR, item('hair', 'variant12'))

    expect(avatar).toEqual({ ...TEST_AVATAR, hair: 'variant12' })
    expect(note).toBeNull()
  })

  it('DoesNotMutateTheBaseAvatar', () => {
    const base = { ...TEST_AVATAR }

    previewWith(base, item('gesture', 'ok'))

    expect(base).toEqual(TEST_AVATAR)
  })

  it('GraphicOnPrintableShirt_NoShirtChange_NoNote', () => {
    const base = { ...TEST_AVATAR, clothes: 'variant10' }

    const { avatar, note } = previewWith(base, item('clothesGraphic', 'galaxy'))

    expect(avatar.clothes).toBe('variant10')
    expect(avatar.clothesGraphic).toBe('galaxy')
    expect(note).toBeNull()
  })

  it('GraphicOnUnprintableShirt_SwitchesToShirt01AndExplains', () => {
    const base = { ...TEST_AVATAR, clothes: 'variant14' }

    const { avatar, note } = previewWith(base, item('clothesGraphic', 'galaxy'))

    expect(avatar.clothes).toBe('variant01')
    expect(avatar.clothesGraphic).toBe('galaxy')
    expect(note).toContain('áo 01')
  })

  it('PreviewingAShirtThatCannotPrint_KeepsTheGraphicOfTheBaseButItIsJustAPreview', () => {
    const base = { ...TEST_AVATAR, clothesGraphic: 'galaxy' }

    const { avatar } = previewWith(base, item('clothes', 'variant14'))

    expect(avatar.clothes).toBe('variant14')
  })
})

describe('GUEST_PREVIEW_AVATAR', () => {
  it('IsAFullAvatarMadeOfFreeLookingBasicsWithTheGreyDefaultBackground', () => {
    expect(GUEST_PREVIEW_AVATAR.background).toBe('d4d4d4')
    expect(GUEST_PREVIEW_AVATAR.glasses).toBeNull()
    expect(GUEST_PREVIEW_AVATAR.beard).toBeNull()
    expect(GUEST_PREVIEW_AVATAR.gesture).toBeNull()
    expect(GUEST_PREVIEW_AVATAR.clothesGraphic).toBeNull()
    for (const slot of ['eyes', 'eyebrows', 'mouth', 'nose', 'hair', 'clothes'] as const) {
      expect(GUEST_PREVIEW_AVATAR[slot]).toBe('variant01') // variant01 của mọi ô bắt buộc đều miễn phí
    }
  })
})
