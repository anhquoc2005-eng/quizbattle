// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, describe, expect, it } from 'vitest'
import type { PracticeSession } from '../../types/practice'
import { PracticeSummary } from './PracticeSummary'

const SESSION: PracticeSession = {
  sessionId: 's1', topic: 'Lịch sử', total: 10, answeredCount: 10, completed: true, current: null, lastResult: null,
  summary: { correctCount: 7, xpGainedInSession: 35, xpGainedToday: 60, remainingToday: 40, resetAt: '2026-11-24T00:00:00+07:00' },
}

afterEach(cleanup)

describe('PracticeSummary', () => {
  it('PracticeSummary_Completed_ShowsXpCorrectCountAndLinkBack', () => {
    render(
      <MemoryRouter>
        <PracticeSummary session={SESSION} />
      </MemoryRouter>,
    )

    expect(screen.getByText('+35 XP trong lượt này, còn 40 XP hôm nay')).toBeTruthy()
    expect(screen.getByText('7 / 10')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Về danh sách ôn' }).getAttribute('href')).toBe('/practice')
  })
})
