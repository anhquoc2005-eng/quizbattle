// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import { getPracticeSession, submitPracticeAnswer } from '../../api/practice'
import { useAuth } from '../../hooks/useAuth'
import type { PracticeAnswerResult, PracticeSession } from '../../types/practice'
import { PracticePage } from './PracticePage'

vi.mock('../../api/practice', () => ({
  getPracticeSession: vi.fn(),
  submitPracticeAnswer: vi.fn(),
  getPracticeRooms: vi.fn(),
  createPracticeSession: vi.fn(),
}))
vi.mock('../../hooks/useAuth', () => ({ useAuth: vi.fn() }))

const SESSION: PracticeSession = {
  sessionId: 's1', topic: 'Lịch sử', total: 3, answeredCount: 0, completed: false,
  current: { questionId: 'q1', index: 0, text: 'Câu hỏi một?', options: ['A1', 'B1', 'C1', 'D1'] },
  lastResult: null, summary: null,
}
const NEXT_SESSION: PracticeSession = {
  ...SESSION, answeredCount: 1,
  current: { questionId: 'q2', index: 1, text: 'Câu hỏi hai?', options: ['A2', 'B2', 'C2', 'D2'] },
  lastResult: { questionId: 'q1', isCorrect: true, correctIndex: 2, xpAwarded: 5, xpReason: null },
}
const RIGHT: PracticeAnswerResult = { isCorrect: true, correctIndex: 2, xpAwarded: 5, xpReason: null, xpGainedToday: 5, remainingToday: 95, completed: false }

function renderPage() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/practice/s1']}>
        <Routes>
          <Route path="/practice/:sessionId" element={<PracticePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

async function pickAndSubmit(optionText: string) {
  fireEvent.click(await screen.findByRole('button', { name: optionText }))
  fireEvent.click(screen.getByRole('button', { name: 'Nộp đáp án' }))
}

beforeEach(() => {
  vi.mocked(getPracticeSession).mockReset().mockResolvedValue(SESSION)
  vi.mocked(submitPracticeAnswer).mockReset()
  vi.mocked(useAuth).mockReturnValue({ user: { id: 'u1', email: 'a@x.com', displayName: 'Alice' } } as ReturnType<typeof useAuth>)
})
afterEach(cleanup)

describe('PracticeQuestion', () => {
  it('PracticeQuestion_WhileSubmitting_DisablesSubmitButton', async () => {
    vi.mocked(submitPracticeAnswer).mockReturnValue(new Promise(() => {}))
    renderPage()
    await pickAndSubmit('C1')

    const button = await screen.findByRole('button', { name: 'Đang gửi…' })
    expect((button as HTMLButtonElement).disabled).toBe(true)
  })

  it('PracticeQuestion_AfterSubmit_ShowsCorrectOptionAndXpMessage', async () => {
    vi.mocked(submitPracticeAnswer).mockResolvedValue({ ...RIGHT, isCorrect: false, correctIndex: 1, xpAwarded: 0, xpReason: 'wrong' })
    renderPage()
    await pickAndSubmit('C1')

    expect(await screen.findByText('Sai, đáp án đúng là lựa chọn 2')).toBeTruthy()
    expect(screen.getByText('Đáp án đúng')).toBeTruthy()
    expect(screen.getByText('Bạn chọn')).toBeTruthy()
  })

  it('PracticeQuestion_AfterSubmit_DoesNotRefetchSessionUntilNext_AndKeepsSubmittedQuestion', async () => {
    vi.mocked(submitPracticeAnswer).mockResolvedValue(RIGHT)
    vi.mocked(getPracticeSession).mockResolvedValueOnce(SESSION).mockResolvedValue(NEXT_SESSION)
    renderPage()
    await pickAndSubmit('C1')

    expect(await screen.findByText('Đúng, +5 XP, còn 95 hôm nay')).toBeTruthy()
    expect(getPracticeSession).toHaveBeenCalledTimes(1)
    expect(screen.getByText('Câu hỏi một?')).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Câu tiếp' }))

    expect(await screen.findByText('Câu hỏi hai?')).toBeTruthy()
    expect(getPracticeSession).toHaveBeenCalledTimes(2)
  })

  it('PracticeQuestion_FirstLoadWithLastResult_ShowsPreviousLine', async () => {
    vi.mocked(getPracticeSession).mockResolvedValue(NEXT_SESSION)
    renderPage()

    expect(await screen.findByText('Câu trước: Đúng, +5 XP')).toBeTruthy()
    expect(screen.getByText('Câu hỏi hai?')).toBeTruthy()
  })

  it('PracticeQuestion_AfterNext_HidesPreviousLine', async () => {
    vi.mocked(submitPracticeAnswer).mockResolvedValue({ ...RIGHT, correctIndex: 0 })
    // Lần tải đầu đã có lastResult; sau "Câu tiếp" server vẫn trả lastResult nhưng dòng phải ẩn.
    vi.mocked(getPracticeSession).mockResolvedValue(NEXT_SESSION)
    renderPage()
    expect(await screen.findByText('Câu trước: Đúng, +5 XP')).toBeTruthy()

    await pickAndSubmit('A2')
    fireEvent.click(await screen.findByRole('button', { name: 'Câu tiếp' }))

    await waitFor(() => expect(getPracticeSession).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Câu tiếp' })).toBeNull())
    expect(screen.queryByText(/Câu trước:/)).toBeNull()
  })

  it('PracticeQuestion_Conflict409_ShowsMessageAndRefetchesSession', async () => {
    vi.mocked(submitPracticeAnswer).mockRejectedValue(new ApiError(409, { code: 'not_current_question' }))
    renderPage()
    await pickAndSubmit('C1')

    expect(await screen.findByText('Câu này không còn là câu hiện tại, đã tải lại lượt ôn')).toBeTruthy()
    await waitFor(() => expect(getPracticeSession).toHaveBeenCalledTimes(2))
  })

  it('PracticeQuestion_NetworkError_AllowsResubmitWithSameSelectedIndex', async () => {
    vi.mocked(submitPracticeAnswer).mockRejectedValueOnce(new TypeError('network')).mockResolvedValueOnce(RIGHT)
    renderPage()
    await pickAndSubmit('C1')

    expect(await screen.findByText(/Không kết nối được máy chủ/)).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Nộp đáp án' }))

    expect(await screen.findByText('Đúng, +5 XP, còn 95 hôm nay')).toBeTruthy()
    expect(submitPracticeAnswer).toHaveBeenNthCalledWith(1, 's1', 'q1', 2)
    expect(submitPracticeAnswer).toHaveBeenNthCalledWith(2, 's1', 'q1', 2)
  })

  it('PracticeQuestion_RateLimited429_KeepsSelectionForRetry', async () => {
    vi.mocked(submitPracticeAnswer).mockRejectedValueOnce(new ApiError(429, { code: 'PRACTICE_RATE_LIMIT' })).mockResolvedValueOnce(RIGHT)
    renderPage()
    await pickAndSubmit('C1')

    expect(await screen.findByText(/thao tác quá nhanh/)).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Nộp đáp án' }))

    await screen.findByText('Đúng, +5 XP, còn 95 hôm nay')
    expect(submitPracticeAnswer).toHaveBeenLastCalledWith('s1', 'q1', 2)
  })

  it('PracticeQuestion_LastAnswerCompleted_RefetchesAndShowsSummaryAfterViewing', async () => {
    const done: PracticeSession = {
      ...SESSION, answeredCount: 3, completed: true, current: null, lastResult: null,
      summary: { correctCount: 2, xpGainedInSession: 10, xpGainedToday: 10, remainingToday: 90, resetAt: '2026-11-24T00:00:00+07:00' },
    }
    vi.mocked(submitPracticeAnswer).mockResolvedValue({ ...RIGHT, completed: true })
    vi.mocked(getPracticeSession).mockResolvedValueOnce(SESSION).mockResolvedValue(done)
    renderPage()
    await pickAndSubmit('C1')

    fireEvent.click(await screen.findByRole('button', { name: 'Xem tổng kết' }))

    expect(await screen.findByText('Còn 90 XP hôm nay')).toBeTruthy()
  })

  it('PracticePage_SessionNotFound_ShowsMessageWithLinkBack', async () => {
    vi.mocked(getPracticeSession).mockRejectedValue(new ApiError(404, null))
    renderPage()

    expect(await screen.findByText('Không tìm thấy lượt ôn')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Về danh sách ôn' })).toBeTruthy()
  })
})
