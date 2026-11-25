import { describe, expect, it } from 'vitest'
import css from './index.css?raw'

// jsdom không áp CSS nên test component không bắt được việc mất rule tên dài; kiểm thẳng index.css.
const text = css.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\s+/g, ' ')

function bodyOf(selector: string): string | undefined {
  const start = text.indexOf(` ${selector} {`)
  if (start === -1) return undefined
  const open = text.indexOf('{', start)
  return text.slice(open + 1, text.indexOf('}', open))
}

describe('tên hiển thị dài (index.css)', () => {
  it('CssIsReadable_SoTheChecksBelowAreNotVacuous', () => {
    expect(text.length).toBeGreaterThan(1000)
    expect(bodyOf('.bar .user-name')).toContain('text-overflow: ellipsis')
  })

  it('NameWrap_BreaksAnywhereAndMayShrink', () => {
    const body = bodyOf('.name-wrap')
    expect(body).toContain('overflow-wrap: anywhere')
    expect(body).toContain('min-width: 0')
  })

  it('NameTruncate_OneLineWithEllipsisAndMayShrink', () => {
    const body = bodyOf('.name-truncate')
    expect(body).toContain('white-space: nowrap')
    expect(body).toContain('text-overflow: ellipsis')
    expect(body).toContain('overflow: hidden')
    expect(body).toContain('min-width: 0')
  })
})
