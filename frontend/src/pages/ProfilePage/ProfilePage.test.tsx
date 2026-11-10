// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getProfile, getRewards } from '../../api/profile'
import type { Profile, RewardHistoryItem } from '../../types/profile'
import { ProfilePage } from './ProfilePage'

vi.mock('../../api/profile', () => ({ getProfile: vi.fn(), getRewards: vi.fn() }))

const PROFILE: Profile = { userId: 'u1', displayName: 'Alice', level: 3, totalXp: 640, xpIntoLevel: 40, xpForNextLevel: 600, coinBalance: 125 }
const REWARD: RewardHistoryItem = {
  roomCode: 'ABC123', topic: 'Lịch sử Việt Nam', playedAt: '2026-11-09T10:15:00Z', score: 2400, rewardRank: 2, xp: 240, coins: 44, rankBonus: 20,
}

function renderPage() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <ProfilePage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getProfile).mockReset()
  vi.mocked(getRewards).mockReset()
})
afterEach(cleanup)

describe('ProfilePage', () => {
  it('ProfilePage_Loading_ShowsLoadingText', () => {
    vi.mocked(getProfile).mockReturnValue(new Promise(() => {}))
    renderPage()

    expect(screen.getByText('Đang tải…')).toBeTruthy()
  })

  it('ProfilePage_ProfileError_ShowsRetryThatRefetches', async () => {
    vi.mocked(getProfile).mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce(PROFILE)
    vi.mocked(getRewards).mockResolvedValue([])
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))

    expect(await screen.findByRole('heading', { name: 'Alice' })).toBeTruthy()
    expect(getProfile).toHaveBeenCalledTimes(2)
  })

  it('ProfilePage_NoRewards_ShowsEmptyHistoryButStillShowsProfile', async () => {
    vi.mocked(getProfile).mockResolvedValue({ ...PROFILE, level: 1, totalXp: 0, xpIntoLevel: 0, xpForNextLevel: 200, coinBalance: 0 })
    vi.mocked(getRewards).mockResolvedValue([])
    renderPage()

    expect(await screen.findByText(/Chưa có trận nào được thưởng/)).toBeTruthy()
    expect(screen.getByText('Cấp 1')).toBeTruthy()
    expect(screen.getByRole('progressbar').getAttribute('aria-valuenow')).toBe('0') // tài khoản 0 XP: thanh rỗng, không NaN
  })

  it('ProfilePage_RewardsError_ShowsRetryInHistoryOnly', async () => {
    vi.mocked(getProfile).mockResolvedValue(PROFILE)
    vi.mocked(getRewards).mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce([REWARD])
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))

    expect(await screen.findByText('Lịch sử Việt Nam')).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Alice' })).toBeTruthy()
  })

  it('ProfilePage_HasData_ShowsLevelXpProgressCoinsAndHistory', async () => {
    vi.mocked(getProfile).mockResolvedValue(PROFILE)
    vi.mocked(getRewards).mockResolvedValue([REWARD])
    renderPage()

    expect(await screen.findByText('Cấp 3')).toBeTruthy()
    expect(screen.getByText('640')).toBeTruthy() // tổng XP
    expect(screen.getByText('125')).toBeTruthy() // Xu
    const bar = screen.getByRole('progressbar')
    expect(bar.getAttribute('aria-valuenow')).toBe('40')
    expect(bar.getAttribute('aria-valuemax')).toBe('600')
    expect(await screen.findByText('Lịch sử Việt Nam')).toBeTruthy()
    expect(screen.getByText(/Hạng 2 · 2400 điểm · \+240 XP · \+64 Xu/)).toBeTruthy() // 44 + 20
  })
})
