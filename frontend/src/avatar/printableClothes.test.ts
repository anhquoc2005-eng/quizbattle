import { Avatar, Style } from '@dicebear/core'
import notionists from '@dicebear/styles/notionists.json'
import { describe, expect, it } from 'vitest'
import { PRINTABLE_CLOTHES, isPrintable } from './printableClothes'

// Bỏ hậu tố id 8 hex của DiceBear (phụ thuộc seed) để so sánh nội dung hình.
const stripIds = (svg: string) => svg.replace(/-[0-9a-f]{8}\b/g, '')

const style = new Style(notionists as unknown as ConstructorParameters<typeof Style>[0])

function render(clothes: string, withGraphic: boolean): string {
  return stripIds(
    new Avatar(style, {
      seed: 'fixed',
      eyesVariant: 'variant01', eyesProbability: 100,
      eyebrowsVariant: 'variant01', eyebrowsProbability: 100,
      mouthVariant: 'variant01', mouthProbability: 100,
      noseVariant: 'variant01', noseProbability: 100,
      hairVariant: 'variant01', hairProbability: 100,
      clothesVariant: clothes, clothesProbability: 100,
      glassesProbability: 0, beardProbability: 0, gestureProbability: 0,
      ...(withGraphic ? { clothesGraphicVariant: 'saturn', clothesGraphicProbability: 100 } : { clothesGraphicProbability: 0 }),
    }).toString(),
  )
}

describe('PRINTABLE_CLOTHES (đối chiếu với đúng phiên bản DiceBear đã ghim)', () => {
  it('SetEqualsClothesWhoseSvgChangesWhenAGraphicIsAdded', () => {
    const allClothes = Object.keys((notionists as unknown as { components: { clothes: { variants: Record<string, unknown> } } }).components.clothes.variants)
    expect(allClothes).toHaveLength(25)

    const measured = allClothes.filter((c) => render(c, true) !== render(c, false)).sort()

    expect(
      measured,
      'Nâng cấp DiceBear làm lệch tập áo in được: cập nhật AvatarCatalog.PrintableClothesVariantKeys ở backend và PRINTABLE_CLOTHES ở frontend.',
    ).toEqual([...PRINTABLE_CLOTHES].sort())
  })

  it('IsVariant01To10', () => {
    expect([...PRINTABLE_CLOTHES]).toEqual(Array.from({ length: 10 }, (_, i) => `variant${String(i + 1).padStart(2, '0')}`))
  })

  it('isPrintable_MatchesSet', () => {
    expect(isPrintable('variant01')).toBe(true)
    expect(isPrintable('variant10')).toBe(true)
    expect(isPrintable('variant11')).toBe(false)
    expect(isPrintable('variant14')).toBe(false)
  })
})
