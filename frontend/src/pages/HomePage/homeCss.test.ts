import { describe, expect, it } from 'vitest'
import css from '../../index.css?raw'

const HERO_CLASSES = ['.hero-char', '.hero-rise', '.hero-timer', '.hero-correct', '.hero-score', '.hero-glow']

/** Nội dung khối @media (prefers-reduced-motion: reduce) (khối có sẵn ở cuối file). */
function reducedBlock(): string {
  const start = css.lastIndexOf('@media (prefers-reduced-motion: reduce)')
  return css.slice(start)
}

describe('index.css trang chủ', () => {
  it('Css_Loaded_IsNotEmpty', () => {
    expect(css.length).toBeGreaterThan(1000)
  })

  it('Css_HeroAnimations_DefineHeroKeyframes', () => {
    for (const name of ['hero-rise', 'hero-drift', 'hero-timer', 'hero-correct', 'hero-score']) {
      expect(css).toContain(`@keyframes ${name}`)
    }
  })

  it('Css_ReducedMotion_DisablesEveryHeroAnimation', () => {
    const block = reducedBlock()
    const m = block.match(/([^{}]*)\{\s*animation:\s*none;\s*\}/g) ?? []
    const disabled = m.join('\n')
    for (const cls of HERO_CLASSES) expect(disabled).toContain(cls)
  })
})
