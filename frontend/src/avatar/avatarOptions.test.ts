import { describe, expect, it } from 'vitest'
import type { Avatar } from '../types/avatar'
import { TEST_AVATAR } from './avatarFixture'
import { AVATAR_SLOTS, DEFAULT_BACKGROUND_HEX, OPTIONAL_SLOTS, REQUIRED_SLOTS, isRequiredSlot } from './avatarSlots'
import { buildOptions, signature } from './avatarOptions'

const FULL: Avatar = {
  eyes: 'variant03',
  eyebrows: 'variant05',
  mouth: 'variant12',
  nose: 'variant07',
  hair: 'variant12',
  clothes: 'variant08',
  glasses: 'variant04',
  beard: 'variant02',
  gesture: 'ok',
  clothesGraphic: 'galaxy',
  background: '2a9d8f',
}

describe('buildOptions', () => {
  it('FullAvatar_SetsVariantAndProbability100PerSlot', () => {
    const options = buildOptions(FULL)

    for (const slot of AVATAR_SLOTS.filter((s) => s !== 'background')) {
      expect(options[`${slot}Variant`]).toBe(FULL[slot])
      expect(options[`${slot}Probability`]).toBe(100)
    }
  })

  it('NullOptionalSlot_SetsProbability0AndNoVariant', () => {
    const options = buildOptions({ ...FULL, glasses: null, gesture: null })

    expect(options.glassesProbability).toBe(0)
    expect(options.gestureProbability).toBe(0)
    expect(options).not.toHaveProperty('glassesVariant')
    expect(options).not.toHaveProperty('gestureVariant')
    expect(options.hairProbability).toBe(100) // ô khác không bị ảnh hưởng
  })

  it('NullBackground_UsesDefaultD4d4d4', () => {
    expect(DEFAULT_BACKGROUND_HEX).toBe('d4d4d4') // khớp AvatarCatalog.DefaultBackgroundHex ở backend (test backend ghim hằng này)
    expect(buildOptions({ ...FULL, background: null }).backgroundColor).toEqual(['d4d4d4'])
  })

  it('BackgroundHexWithoutHash', () => {
    expect(buildOptions(FULL).backgroundColor).toEqual(['2a9d8f'])
  })

  it('SeedIsStableJoinOfSlotKeys', () => {
    expect(buildOptions(FULL).seed).toBe(signature(FULL))
    expect(buildOptions(FULL).seed).toBe(buildOptions({ ...FULL }).seed)
  })

  it('BuildsOnlyKnownOptionKeys_NoSizeOrOtherDefaults', () => {
    const keys = Object.keys(buildOptions(FULL)).sort()
    const expected = [
      'seed',
      'backgroundColor',
      ...AVATAR_SLOTS.filter((s) => s !== 'background').flatMap((s) => [`${s}Variant`, `${s}Probability`]),
    ].sort()

    expect(keys).toEqual(expected)
  })
})

describe('signature', () => {
  it('SameAvatarSameString_DifferentAvatarDifferent', () => {
    expect(signature(FULL)).toBe(signature({ ...FULL }))
    for (const slot of AVATAR_SLOTS) {
      const changed = { ...FULL, [slot]: slot === 'glasses' ? null : 'variant99' }
      expect(signature(changed)).not.toBe(signature(FULL))
    }
  })

  it('EmptyAndMissingOptionalSlotsDiffer', () => {
    expect(signature({ ...FULL, glasses: null })).not.toBe(signature({ ...FULL, glasses: 'variant01' }))
  })

  it('ListsEverySlotInFixedOrder', () => {
    const sig = signature(TEST_AVATAR)

    expect(sig.split('|').map((p) => p.split('.')[0])).toEqual([...AVATAR_SLOTS])
  })
})

describe('avatarSlots', () => {
  it('SixRequiredFiveOptional_NoOverlap', () => {
    expect(REQUIRED_SLOTS).toEqual(['eyes', 'eyebrows', 'mouth', 'nose', 'hair', 'clothes'])
    expect(OPTIONAL_SLOTS).toEqual(['glasses', 'beard', 'gesture', 'clothesGraphic', 'background'])
    expect(AVATAR_SLOTS).toHaveLength(11)
    expect(isRequiredSlot('hair')).toBe(true)
    expect(isRequiredSlot('beard')).toBe(false)
  })
})
