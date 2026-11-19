// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import { getAiQuota } from '../../api/aiQuota'
import { generateQuestions, getQuestions } from '../../api/questions'
import type { AiQuota } from '../../types/aiQuota'
import type { Question } from '../../types/question'
import { GenerateQuestionsPanel } from './GenerateQuestionsPanel'
import { QuestionForm } from './QuestionForm'

vi.mock('../../api/aiQuota', () => ({ getAiQuota: vi.fn() }))
vi.mock('../../api/questions', () => ({ generateQuestions: vi.fn(), getQuestions: vi.fn() }))

const RESET_AT = '2026-11-20T00:00:00+07:00'
const quota = (patch: Partial<AiQuota> = {}): AiQuota => ({
  dailyLimit: 20, used: 3, remaining: 17, globalExhausted: false, resetAt: RESET_AT, ...patch,
})

function renderPanel(children: React.ReactNode = <GenerateQuestionsPanel code="ABCDEF" />) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(<QueryClientProvider client={client}>{children}</QueryClientProvider>)
}

const submitButton = () => screen.getByRole('button', { name: /Sinh câu hỏi|AI đang soạn/ })
const countText = (text: string) => screen.getByText((_, el) => el?.tagName === 'P' && el.textContent === text)

describe('GenerateQuestionsPanel: số lượt AI', () => {
  beforeEach(() => {
    vi.mocked(getQuestions).mockResolvedValue([] as Question[])
  })
  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
  })

  it('đang tải số lượt: hiện chữ chờ', () => {
    vi.mocked(getAiQuota).mockReturnValue(new Promise(() => undefined))
    renderPanel()

    expect(screen.getByText('Đang tải số lượt AI…')).toBeTruthy()
  })

  it('lỗi tải số lượt: có nút Thử lại và nút Sinh VẪN bấm được', async () => {
    vi.mocked(getAiQuota).mockRejectedValueOnce(new ApiError(500, null)).mockResolvedValueOnce(quota())
    renderPanel()

    expect(await screen.findByText(/Không tải được số lượt AI/)).toBeTruthy()
    expect((submitButton() as HTMLButtonElement).disabled).toBe(false)

    fireEvent.click(screen.getByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText((_, el) => el?.textContent === 'Còn 17/20 lượt AI hôm nay' && el.tagName === 'P')).toBeTruthy()
  })

  it('còn lượt: hiện "Còn 17/20 lượt AI hôm nay" và nút bật', async () => {
    vi.mocked(getAiQuota).mockResolvedValue(quota())
    renderPanel()

    await waitFor(() => expect(countText('Còn 17/20 lượt AI hôm nay')).toBeTruthy())
    expect((submitButton() as HTMLButtonElement).disabled).toBe(false)
    expect(screen.queryByRole('status')).toBeNull()
  })

  it('hết lượt cá nhân: nút tắt + thông báo giờ làm mới cố định 00:00 (giờ Việt Nam)', async () => {
    vi.mocked(getAiQuota).mockResolvedValue(quota({ used: 20, remaining: 0 }))
    renderPanel()

    expect(await screen.findByRole('status')).toHaveProperty(
      'textContent',
      'Bạn đã dùng hết lượt AI hôm nay, làm mới lúc 00:00 (giờ Việt Nam). Bạn vẫn thêm câu hỏi thủ công được.',
    )
    expect((submitButton() as HTMLButtonElement).disabled).toBe(true)
  })

  it('hệ thống hết lượt dù cá nhân còn: nút tắt + thông báo KHÁC thông báo cá nhân', async () => {
    vi.mocked(getAiQuota).mockResolvedValue(quota({ globalExhausted: true }))
    renderPanel()

    const status = await screen.findByRole('status')
    expect(status.textContent).toContain('Hệ thống AI hôm nay đã hết lượt')
    expect(status.textContent).not.toContain('Bạn đã dùng hết')
    expect((submitButton() as HTMLButtonElement).disabled).toBe(true)
  })

  it('hết cả hai: báo kiểu hệ thống (khớp mã server AI_DAILY_LIMIT_GLOBAL)', async () => {
    vi.mocked(getAiQuota).mockResolvedValue(quota({ globalExhausted: true, used: 20, remaining: 0 }))
    renderPanel()

    expect((await screen.findByRole('status')).textContent).toContain('Hệ thống AI hôm nay đã hết lượt')
  })

  it('nhận 429 AI_DAILY_LIMIT_USER khi gửi: tải lại số lượt và hiện thông báo hết lượt có giờ làm mới', async () => {
    vi.mocked(getAiQuota).mockResolvedValueOnce(quota({ used: 19, remaining: 1 })).mockResolvedValue(quota({ used: 20, remaining: 0 }))
    vi.mocked(generateQuestions).mockRejectedValue(
      new ApiError(429, { code: 'AI_DAILY_LIMIT_USER', resetAt: RESET_AT, title: 'Bạn đã dùng hết lượt AI hôm nay.' }),
    )
    renderPanel()
    await waitFor(() => expect(countText('Còn 1/20 lượt AI hôm nay')).toBeTruthy())

    fireEvent.click(submitButton())

    expect((await screen.findByRole('status')).textContent).toContain('làm mới lúc 00:00 (giờ Việt Nam)')
    expect(getAiQuota).toHaveBeenCalledTimes(2) // đã invalidate sau lỗi
    expect(countText('Còn 0/20 lượt AI hôm nay')).toBeTruthy()
    expect((submitButton() as HTMLButtonElement).disabled).toBe(true)
  })

  it('nhận 429 AI_RATE_LIMIT: thông báo dồn dập, không nhắc ngày/giờ làm mới, nút vẫn bật', async () => {
    vi.mocked(getAiQuota).mockResolvedValue(quota())
    vi.mocked(generateQuestions).mockRejectedValue(new ApiError(429, { code: 'AI_RATE_LIMIT', title: 'Quá nhiều yêu cầu' }))
    renderPanel()
    await waitFor(() => expect(countText('Còn 17/20 lượt AI hôm nay')).toBeTruthy())

    fireEvent.click(submitButton())

    const alert = await screen.findByRole('alert')
    expect(alert.textContent).toBe('Bạn sinh câu hỏi quá nhanh, thử lại sau ít phút.')
    expect(alert.textContent).not.toContain('00:00')
    await waitFor(() => expect((submitButton() as HTMLButtonElement).disabled).toBe(false))
  })

  it('thành công: tải lại số lượt', async () => {
    vi.mocked(getAiQuota).mockResolvedValueOnce(quota()).mockResolvedValue(quota({ used: 4, remaining: 16 }))
    vi.mocked(generateQuestions).mockResolvedValue({ requested: 5, created: 5, questions: [] })
    renderPanel()
    await waitFor(() => expect(countText('Còn 17/20 lượt AI hôm nay')).toBeTruthy())

    fireEvent.click(submitButton())

    await waitFor(() => expect(countText('Còn 16/20 lượt AI hôm nay')).toBeTruthy())
  })

  it('hết lượt AI không chặn form thêm câu hỏi thủ công', async () => {
    vi.mocked(getAiQuota).mockResolvedValue(quota({ used: 20, remaining: 0 }))
    renderPanel(
      <>
        <GenerateQuestionsPanel code="ABCDEF" />
        <QuestionForm code="ABCDEF" mode="add" onDone={() => undefined} />
      </>,
    )

    await screen.findByRole('status')
    expect((submitButton() as HTMLButtonElement).disabled).toBe(true)
    const manualSubmit = screen.getAllByRole('button').find((b) => /^(Lưu|Thêm câu)/.test(b.textContent ?? ''))
    expect(manualSubmit).toBeDefined()
    expect((manualSubmit as HTMLButtonElement).disabled).toBe(false)
  })
})
