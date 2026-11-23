// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPracticeSession, getPracticeRooms } from '../../api/practice'
import { useAuth } from '../../hooks/useAuth'
import type { PracticeRoomsResponse } from '../../types/practice'
import { PracticeRooms } from './PracticeRooms'

vi.mock('../../api/practice', () => ({ getPracticeRooms: vi.fn(), createPracticeSession: vi.fn() }))
vi.mock('../../hooks/useAuth', () => ({ useAuth: vi.fn() }))

const DATA: PracticeRoomsResponse = {
  xpPerCorrect: 5, dailyXpCap: 100, xpGainedToday: 35, remainingToday: 65, resetAt: '2026-11-24T00:00:00+07:00',
  rooms: [
    { roomCode: 'ABC123', topic: 'Lịch sử', finishedAt: '2026-11-20T10:00:00Z', questionCount: 10, role: 'player', openSessionId: null },
    { roomCode: 'XYZ789', topic: 'Địa lý', finishedAt: '2026-11-19T10:00:00Z', questionCount: 8, role: 'host', openSessionId: 'open-1' },
  ],
}

function renderPage() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/practice']}>
        <Routes>
          <Route path="/practice" element={<PracticeRooms />} />
          <Route path="/practice/:sessionId" element={<p>Đang ở phiên ôn</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getPracticeRooms).mockReset()
  vi.mocked(createPracticeSession).mockReset()
  vi.mocked(useAuth).mockReturnValue({ user: { id: 'u1', email: 'a@x.com', displayName: 'Alice' } } as ReturnType<typeof useAuth>)
})
afterEach(cleanup)

describe('PracticeRooms', () => {
  it('PracticeRooms_Loading_ShowsLoadingText', () => {
    vi.mocked(getPracticeRooms).mockReturnValue(new Promise(() => {}))
    renderPage()

    expect(screen.getByText('Đang tải…')).toBeTruthy()
  })

  it('PracticeRooms_Error_ShowsRetryThatRefetches', async () => {
    vi.mocked(getPracticeRooms).mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce(DATA)
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))

    expect(await screen.findByText('Lịch sử')).toBeTruthy()
    expect(getPracticeRooms).toHaveBeenCalledTimes(2)
  })

  it('PracticeRooms_NoRooms_ShowsEmptyMessage', async () => {
    vi.mocked(getPracticeRooms).mockResolvedValue({ ...DATA, rooms: [] })
    renderPage()

    expect(await screen.findByText(/Chưa có phòng nào để ôn/)).toBeTruthy()
  })

  it('PracticeRooms_WithData_ShowsRoomsAndDailyXp', async () => {
    vi.mocked(getPracticeRooms).mockResolvedValue(DATA)
    renderPage()

    expect(await screen.findByText('Hôm nay 35/100 XP')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Ôn Lịch sử' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Tiếp tục Địa lý' })).toBeTruthy()
  })

  it('PracticeRooms_StartClicked_PostsThenNavigatesToSession', async () => {
    vi.mocked(getPracticeRooms).mockResolvedValue(DATA)
    vi.mocked(createPracticeSession).mockResolvedValue({ sessionId: 'new-1', total: 10 })
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Ôn Lịch sử' }))

    expect(await screen.findByText('Đang ở phiên ôn')).toBeTruthy()
    expect(createPracticeSession).toHaveBeenCalledWith('ABC123')
  })

  it('PracticeRooms_ContinueClicked_NavigatesWithoutPost', async () => {
    vi.mocked(getPracticeRooms).mockResolvedValue(DATA)
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Tiếp tục Địa lý' }))

    expect(await screen.findByText('Đang ở phiên ôn')).toBeTruthy()
    expect(createPracticeSession).not.toHaveBeenCalled()
  })
})
