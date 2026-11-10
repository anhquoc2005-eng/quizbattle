// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getXpLeaderboard } from '../../api/leaderboards'
import { XpRankingPage } from './XpRankingPage'

vi.mock('../../api/leaderboards', () => ({ getXpLeaderboard: vi.fn() }))

function renderPage() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <XpRankingPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getXpLeaderboard).mockReset()
})
afterEach(cleanup)

describe('XpRankingPage', () => {
  it('XpRankingPage_Loading_ShowsLoadingText', () => {
    vi.mocked(getXpLeaderboard).mockReturnValue(new Promise(() => {}))
    renderPage()

    expect(screen.getByText('Đang tải…')).toBeTruthy()
  })

  it('XpRankingPage_Error_ShowsRetryThatRefetches', async () => {
    vi.mocked(getXpLeaderboard).mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce([])
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))

    expect(await screen.findByText(/Chưa có ai có XP/)).toBeTruthy()
    expect(getXpLeaderboard).toHaveBeenCalledTimes(2)
  })

  it('XpRankingPage_Empty_ShowsEmptyState', async () => {
    vi.mocked(getXpLeaderboard).mockResolvedValue([])
    renderPage()

    expect(await screen.findByText(/Chưa có ai có XP/)).toBeTruthy()
  })

  it('XpRankingPage_HasData_ShowsRankNameLevelAndXp', async () => {
    vi.mocked(getXpLeaderboard).mockResolvedValue([
      { rank: 1, displayName: 'Alice', level: 7, totalXp: 4520 },
      { rank: 2, displayName: 'Bob', level: 2, totalXp: 250 },
    ])
    renderPage()

    expect(await screen.findByText('Alice')).toBeTruthy()
    expect(screen.getByText('Cấp 7')).toBeTruthy()
    expect(screen.getByText('4520 XP')).toBeTruthy()
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
  })
})
