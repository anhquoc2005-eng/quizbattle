import { describe, expect, it } from 'vitest'
import cta from './Cta.tsx?raw'
import footer from './SiteFooter.tsx?raw'
import hero from './Hero.tsx?raw'
import how from './HowItWorks.tsx?raw'

// Chống xanh suông: nguồn phải đọc được và chứa một class chắc chắn có trong file.
// Khoảng cách, chiều cao tương đối và chữ có bị cắt hay không: chỉ đo bằng trình duyệt (jsdom không có layout).
function readable(src: string, sure: string) {
  expect(src.length).toBeGreaterThan(100)
  expect(src).toContain(sure)
}

describe('bố cục trang chủ trên màn desktop thấp', () => {
  it('Cta_Source_HasNoViewportHeightMinHeight', () => {
    readable(cta, 'bg-ink')
    expect(cta).not.toMatch(/min-h-\[[^\]]*v?h[^\]]*\]/)
  })

  it('HowItWorks_Source_DoesNotPushDescriptionToBottom', () => {
    readable(how, 'home-fade')
    expect(how).not.toContain('justify-between')
    expect(how).not.toMatch(/min-h-\[[^\]]*v?h[^\]]*\]/)
  })

  it('SiteFooter_Source_DoesNotShiftWordmarkOutOfFooter', () => {
    readable(footer, 'overflow-hidden')
    expect(footer).not.toContain('translate-y')
  })

  it('Hero_Source_LimitsFontSizeByViewportHeightToo', () => {
    readable(hero, 'hero-rise')
    expect(hero).toMatch(/text-\[clamp\([^\]]*min\([^\]]*vw,[^\]]*vh\)/)
  })
})
