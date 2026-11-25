import { describe, expect, it } from 'vitest'
import { getRedirectPath } from './redirectPath'

describe('getRedirectPath', () => {
  it('GetRedirectPath_FromWithSearch_KeepsPathAndSearch', () => {
    expect(getRedirectPath({ from: { pathname: '/join', search: '?code=ABC123' } })).toBe('/join?code=ABC123')
  })

  it('GetRedirectPath_NoState_ReturnsDashboard', () => {
    expect(getRedirectPath(undefined)).toBe('/dashboard')
    expect(getRedirectPath(null)).toBe('/dashboard')
  })

  it('GetRedirectPath_ProtocolRelativePath_ReturnsDashboard', () => {
    expect(getRedirectPath({ from: { pathname: '//evil.com' } })).toBe('/dashboard')
  })

  it('GetRedirectPath_PathWithoutLeadingSlash_ReturnsDashboard', () => {
    expect(getRedirectPath({ from: { pathname: 'evil.com' } })).toBe('/dashboard')
  })
})
