import { describe, expect, it } from 'vitest'
import css from '../../index.css?raw'

const HOME_ANIMATED = ['.hero-rise', '.home-mask > span', '.home-fade', '.home-timer', '.home-score']
const REMOVED = ['.hero-glow', '.hero-char', '.hero-correct', 'hero-drift']

/** Nội dung khối @media (prefers-reduced-motion: reduce) (khối có sẵn ở cuối file). */
function reducedBlock(): string {
  return css.slice(css.lastIndexOf('@media (prefers-reduced-motion: reduce)'))
}

describe('index.css trang chủ', () => {
  it('Css_Loaded_IsNotEmpty', () => {
    expect(css.length).toBeGreaterThan(1000)
  })

  it('Css_HomeAnimations_DefineKeyframes', () => {
    for (const name of ['hero-rise', 'home-mask', 'home-fade', 'home-timer', 'home-score']) {
      expect(css).toContain(`@keyframes ${name}`)
    }
  })

  it('Css_ReducedMotion_DisablesEveryHomeAnimationAndRevealTransition', () => {
    const block = reducedBlock()
    const disabled = (block.match(/([^{}]*)\{\s*animation:\s*none;\s*\}/g) ?? []).join('\n')
    for (const cls of HOME_ANIMATED) expect(disabled).toContain(cls)
    expect(block).toMatch(/\[data-reveal\]\s*\{\s*transition:\s*none;/)
  })

  // Nội dung chỉ được ẩn trước khi vào màn hình khi script đã đặt data-js: lỗi script thì vẫn đọc được.
  it('Css_RevealHiding_RequiresDataJsAncestor', () => {
    const hidingRules = [...css.matchAll(/([^{}]*)\{([^{}]*opacity:\s*0[^{}]*)\}/g)].filter(([, sel]) =>
      /data-reveal/.test(sel ?? ''),
    )
    expect(hidingRules.length).toBeGreaterThan(0)
    for (const [, sel] of hidingRules) expect(sel).toContain('[data-js]')
  })

  // Rule ngoài @layer thắng class Tailwind: display ở đây từng đè `flex` của .hero-actions (nút dính nhau).
  it('Css_HeroRise_DoesNotDeclareDisplay', () => {
    const rules = [...css.matchAll(/([^{}]*)\{([^{}]*)\}/g)].filter(([, sel]) => /\.hero-rise\b/.test(sel ?? ''))
    expect(rules.length).toBeGreaterThan(0)
    for (const [, , body] of rules) expect(body).not.toMatch(/\bdisplay\s*:/)
  })

  it('Css_Home_HasNoGlowOrRemovedHeroClasses', () => {
    for (const name of REMOVED) expect(css).not.toContain(name)
  })
})
