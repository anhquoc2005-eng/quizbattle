import { describe, expect, it } from 'vitest'
import css from './index.css?raw'

// jsdom không áp CSS nên test component không bắt được việc mất rule của menu điện thoại; kiểm thẳng index.css.
const text = css.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\s+/g, ' ')

/** Thân rule `selector { ... }` đầu tiên trong `source` (selector phải đứng đầu rule, không phải đuôi của selector dài hơn). */
function bodyOf(selector: string, source = text): string | undefined {
  for (const start of [source.indexOf(` ${selector} {`), source.indexOf(`}${selector} {`)]) {
    if (start === -1) continue
    const open = source.indexOf('{', start)
    return source.slice(open + 1, source.indexOf('}', open))
  }
  return undefined
}

const MOBILE_QUERY = '@media (max-width: 640px) {'
const mobileStart = text.indexOf(MOBILE_QUERY)
const nextQuery = text.indexOf('@media', mobileStart + 1)
const mobileBlock = text.slice(mobileStart, nextQuery === -1 ? undefined : nextQuery)

describe('menu điện thoại (index.css)', () => {
  it('CssIsReadable_SoTheChecksBelowAreNotVacuous', () => {
    expect(text.length).toBeGreaterThan(1000)
    expect(mobileStart).toBeGreaterThan(-1)
    expect(bodyOf('.bar .hide-sm')).toContain('display: inline')
  })

  it('ShowSm_HiddenByDefault_AndShownOnlyInsideMobileBreakpoint', () => {
    expect(bodyOf('.bar .show-sm')).toContain('display: none')
    expect(bodyOf('.bar .show-sm', mobileBlock)).toContain('display: block')
  })

  it('UserName_LongNameIsTruncatedWithEllipsisOnOneLine', () => {
    const body = bodyOf('.bar .user-name')
    expect(body).toContain('text-overflow: ellipsis')
    expect(body).toContain('white-space: nowrap')
    expect(bodyOf('.bar .user-name', mobileBlock)).toContain('max-width: 9ch')
  })

  it('MarkText_HiddenOnlyWhenSignedInOnNarrowScreens', () => {
    const start = text.indexOf('@media (max-width: 400px) {')
    expect(start).toBeGreaterThan(-1)
    const block = text.slice(start, text.indexOf('@media', start + 1) === -1 ? undefined : text.indexOf('@media', start + 1))
    expect(bodyOf('.bar.signed-in .mark-text', block)).toContain('display: none')
    expect(bodyOf('.bar .mark-text')).toBeUndefined() // không có rule ẩn chữ không điều kiện
  })

  it('MenuPanel_IsAbsolutelyPositioned', () => {
    expect(bodyOf('.bar .menu-panel')).toContain('position: absolute')
  })
})
