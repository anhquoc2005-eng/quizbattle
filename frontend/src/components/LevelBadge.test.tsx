// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { LevelBadge } from './LevelBadge'

afterEach(cleanup)

describe('LevelBadge', () => {
  it('LevelBadge_HasLevel_ShowsLevelText', () => {
    render(<LevelBadge level={3} />)

    expect(screen.getByText('Cấp 3')).toBeTruthy()
  })

  it('LevelBadge_LevelNull_RendersNothing', () => {
    const { container } = render(<LevelBadge level={null} />)

    expect(container.textContent).toBe('')
  })
})
