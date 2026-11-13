// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { peekAvatarDataUri, renderAvatarDataUri } from '../avatar/avatarRenderer'
import { Avatar } from './Avatar'

vi.mock('../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn() }))

const URI = 'data:image/svg+xml;charset=utf-8,%3Csvg%3E%3C%2Fsvg%3E'

beforeEach(() => {
  vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue(undefined)
  vi.mocked(renderAvatarDataUri).mockReset()
})
afterEach(cleanup)

describe('Avatar', () => {
  it('ShowsPlaceholderWhileTheRendererLoads_ThenTheImage', async () => {
    let resolve: (uri: string) => void = () => undefined
    vi.mocked(renderAvatarDataUri).mockReturnValue(new Promise((r) => (resolve = r)))

    const { container } = render(<Avatar avatar={TEST_AVATAR} size="sm" />)

    expect(container.querySelector('img')).toBeNull()
    expect(container.querySelector('[data-state="loading"]')).not.toBeNull()

    resolve(URI)
    await waitFor(() => expect(container.querySelector('img')).not.toBeNull())
    expect(container.querySelector('img')?.getAttribute('src')).toBe(URI)
    expect(container.querySelector('[data-state="loading"]')).toBeNull()
  })

  it('UsesCachedImageImmediately_NoPlaceholderNoRender', () => {
    vi.mocked(peekAvatarDataUri).mockReturnValue(URI)

    const { container } = render(<Avatar avatar={TEST_AVATAR} size="lg" />)

    expect(container.querySelector('img')?.getAttribute('src')).toBe(URI)
    expect(container.querySelector('[data-state="loading"]')).toBeNull()
    expect(renderAvatarDataUri).not.toHaveBeenCalled()
  })

  it('RendererFailure_KeepsPlaceholderWithoutCrashing', async () => {
    vi.mocked(renderAvatarDataUri).mockRejectedValue(new Error('chunk load failed'))

    const { container } = render(<Avatar avatar={TEST_AVATAR} size="sm" />)

    await waitFor(() => expect(renderAvatarDataUri).toHaveBeenCalled())
    expect(container.querySelector('img')).toBeNull()
    expect(container.querySelector('[data-state="loading"]')).not.toBeNull()
  })

  it('Decorative_ByDefault_HasEmptyAltAndIsHiddenFromScreenReaders', () => {
    vi.mocked(peekAvatarDataUri).mockReturnValue(URI)

    const { container } = render(<Avatar avatar={TEST_AVATAR} size="sm" />)

    const img = container.querySelector('img')!
    expect(img.getAttribute('alt')).toBe('')
    expect(img.getAttribute('aria-hidden')).toBe('true')
  })

  it('WithLabel_HasMeaningfulAlt', () => {
    vi.mocked(peekAvatarDataUri).mockReturnValue(URI)

    render(<Avatar avatar={TEST_AVATAR} size="lg" label="Nhân vật của bạn" />)

    expect(screen.getByAltText('Nhân vật của bạn')).not.toBeNull()
  })

  it('TwoSizes_SmallAndLarge', () => {
    vi.mocked(peekAvatarDataUri).mockReturnValue(URI)

    const small = render(<Avatar avatar={TEST_AVATAR} size="sm" />)
    const smallClass = small.container.querySelector('img')!.className
    small.unmount()
    const large = render(<Avatar avatar={TEST_AVATAR} size="lg" />)
    const largeClass = large.container.querySelector('img')!.className

    expect(smallClass).toContain('size-7') // 28 px
    expect(largeClass).toContain('size-40') // 160 px
  })

  it('ImageOnly_NoInlineSvgInjection', () => {
    vi.mocked(peekAvatarDataUri).mockReturnValue(URI)

    const { container } = render(<Avatar avatar={TEST_AVATAR} size="sm" />)

    expect(container.querySelector('svg')).toBeNull()
    expect(container.querySelector('img')).not.toBeNull()
  })

  it('ChangedAvatar_ShowsTheNewImage', async () => {
    vi.mocked(renderAvatarDataUri).mockImplementation((a) => Promise.resolve(`data:image/svg+xml,${a.hair}`))

    const { container, rerender } = render(<Avatar avatar={TEST_AVATAR} size="sm" />)
    await waitFor(() => expect(container.querySelector('img')?.getAttribute('src')).toBe('data:image/svg+xml,variant01'))

    rerender(<Avatar avatar={{ ...TEST_AVATAR, hair: 'variant05' }} size="sm" />)

    await waitFor(() => expect(container.querySelector('img')?.getAttribute('src')).toBe('data:image/svg+xml,variant05'))
  })
})
