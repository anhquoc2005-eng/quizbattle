import { describe, expect, it } from 'vitest'
import css from './index.css?raw'

// jsdom không áp dụng media query của stylesheet, nên kiểm bằng cách đọc chính index.css: mọi rule có `animation:` hoặc
// `transition:` ngoài khối @media (prefers-reduced-motion: reduce) phải có dòng tắt tương ứng TRONG khối đó (FR-017, SC-006).

interface Rule {
  selector: string
  body: string
  reduced: boolean
}

/** Các rule "lá" (không chứa rule con) của stylesheet, bỏ comment và bỏ bước trong @keyframes. */
function leafRules(source: string): Rule[] {
  const text = source.replace(/\/\*[\s\S]*?\*\//g, '')
  const rules: Rule[] = []
  const stack: { prelude: string; hasChild: boolean; bodyStart: number }[] = []
  let preludeStart = 0

  for (let i = 0; i < text.length; i++) {
    if (text[i] === '{') {
      const parent = stack[stack.length - 1]
      if (parent) parent.hasChild = true
      stack.push({ prelude: text.slice(preludeStart, i).trim(), hasChild: false, bodyStart: i + 1 })
      preludeStart = i + 1
    } else if (text[i] === '}') {
      const block = stack.pop()
      if (block && !block.hasChild && !stack.some((b) => b.prelude.startsWith('@keyframes'))) {
        rules.push({
          selector: block.prelude,
          body: text.slice(block.bodyStart, i),
          reduced: stack.some((b) => /prefers-reduced-motion\s*:\s*reduce/.test(b.prelude)),
        })
      }
      preludeStart = i + 1
    }
  }

  return rules
}

const selectorsOf = (rule: Rule) => rule.selector.split(',').map((s) => s.trim().replace(/\s+/g, ' '))
// `animation-delay:` không khớp (sau "animation" là dấu gạch) - chỉ rule thực sự bật animation/transition.
const MOTION = /(^|[;\s])(animation|transition)\s*:/
const rules = leafRules(css)
const motionSelectors = rules.filter((r) => !r.reduced && MOTION.test(r.body)).flatMap(selectorsOf)
const reducedRules = rules.filter((r) => r.reduced)
const disabledSelectors = new Set(
  reducedRules.filter((r) => /(animation|transition)\s*:\s*none|display\s*:\s*none/.test(r.body)).flatMap(selectorsOf),
)

describe('prefers-reduced-motion', () => {
  it('ParserSeesTheKnownAnimatedSelectors_SoTheAuditIsNotVacuous', () => {
    expect(motionSelectors).toEqual(
      expect.arrayContaining(['.btn', '.desk-figure', '.desk-badge', ".desk[data-status='correct'] .desk-figure", '.podium-place', '.confetti i']),
    )
  })

  it('EveryAnimatedOrTransitionedSelector_IsDisabledInsideTheReducedMotionBlock', () => {
    const missing = motionSelectors.filter((selector) => !disabledSelectors.has(selector))

    expect(missing).toEqual([])
  })

  it('ReducedMotionBlock_OnlyHidesTheConfetti_NeverRealContent', () => {
    const hidden = reducedRules.filter((r) => /display\s*:\s*none/.test(r.body)).flatMap(selectorsOf)

    expect(hidden).toEqual(['.confetti'])
  })
})
