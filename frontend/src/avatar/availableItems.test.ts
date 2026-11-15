import { describe, expect, it } from 'vitest'
import type { AvatarItem, AvatarTier } from '../types/avatar'
import { availableItems } from './availableItems'

const item = (key: string, tier: AvatarTier): AvatarItem => {
  const [slot, variantKey] = key.split('.') as [AvatarItem['slot'], string]
  return { key, slot, variantKey, name: key, tier, price: tier === 'free' ? 0 : 50 }
}

const ITEMS = [
  item('hair.variant01', 'free'),
  item('hair.variant03', 'common'),
  item('hair.variant04', 'common'),
  item('hair.variant62', 'free'),
  item('gesture.ok', 'rare'),
]

describe('availableItems', () => {
  it('FreeAndOwned_InCatalogOrder_NeverUnownedPaid', () => {
    expect(availableItems(ITEMS, ['hair.variant04'], 'hair').map((i) => i.key)).toEqual(['hair.variant01', 'hair.variant04', 'hair.variant62'])
  })

  it('NothingOwned_OnlyFree', () => {
    expect(availableItems(ITEMS, [], 'hair').map((i) => i.key)).toEqual(['hair.variant01', 'hair.variant62'])
    expect(availableItems(ITEMS, [], 'gesture')).toEqual([])
  })

  it('OwnershipOfAnotherSlotDoesNotLeak', () => {
    expect(availableItems(ITEMS, ['gesture.ok'], 'hair').some((i) => i.key === 'gesture.ok')).toBe(false)
  })
})
