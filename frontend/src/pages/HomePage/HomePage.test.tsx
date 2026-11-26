// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getHealth } from '../../api/health'
import { getMe } from '../../api/auth'
import { clearAuth, saveAuth } from '../../api/tokenStorage'
import { AuthProvider } from '../../components/AuthProvider'
import type { AuthResponse } from '../../types/auth'
import { HomePage } from './HomePage'

vi.mock('../../api/health', () => ({ getHealth: vi.fn() }))
vi.mock('../../api/auth', () => ({ getMe: vi.fn(), login: vi.fn(), register: vi.fn() }))

const USER = { id: 'u1', email: 'a@b.c', displayName: 'Alice' }
const AUTH: AuthResponse = { token: 'jwt', expiresAt: new Date(Date.now() + 60_000).toISOString(), user: USER }

function LocationProbe() {
  const location = useLocation()
  return <p data-testid="location">{location.pathname + location.search}</p>
}

function renderHome() {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/join" element={<LocationProbe />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function codeInput() {
  return screen.getByLabelText('Mã phòng') as HTMLInputElement
}

beforeEach(() => {
  clearAuth()
  vi.mocked(getHealth).mockReset().mockRejectedValue(new Error('down'))
  vi.mocked(getMe).mockReset().mockResolvedValue(USER)
})
afterEach(cleanup)

describe('HomePage', () => {
  it('Render_Always_PingsHealthOnceAndShowsNoStatusCard', async () => {
    renderHome()
    await waitFor(() => expect(getHealth).toHaveBeenCalledTimes(1))
    expect(screen.queryByText('Trạng thái hệ thống')).toBeNull()
  })

  it('Render_Always_H1IsTheSentence', () => {
    renderHome()
    expect(screen.getByRole('heading', { level: 1, name: 'Vào trận bằng mã' })).toBeTruthy()
  })

  it('Render_LoggedOut_ShowsCreateRoomToRegisterAndSharedLinks', async () => {
    renderHome()
    expect((await screen.findByRole('link', { name: 'Tạo phòng' })).getAttribute('href')).toBe('/register')
    for (const name of ['Cửa hàng', 'Xếp hạng']) expect(screen.getAllByRole('link', { name })[0]).toBeTruthy()
    expect(screen.getAllByRole('link', { name: 'Đăng nhập' }).length).toBeGreaterThan(0)
  })

  it('Render_LoggedIn_CreateRoomGoesToDashboardAndLoginIsGone', async () => {
    saveAuth(AUTH)
    renderHome()
    await waitFor(() => expect(screen.getAllByRole('link', { name: 'Tạo phòng' })[0]?.getAttribute('href')).toBe('/dashboard'))
    expect(screen.queryByRole('link', { name: 'Đăng nhập' })).toBeNull()
  })

  it('Render_Always_AllRevealTargetsAreVisibleWhenObserverIsMissing', () => {
    renderHome()
    const targets = document.querySelectorAll<HTMLElement>('[data-reveal]')
    expect(targets.length).toBeGreaterThan(0)
    for (const el of targets) expect(el.dataset.in).toBe('1')
  })
})

describe('Ô mã phòng', () => {
  it('RoomCode_FormAttributes_FallBackToPlainGetToJoin', () => {
    renderHome()
    const form = codeInput().closest('form')!
    expect(form.getAttribute('action')).toBe('/join')
    expect(form.getAttribute('method')).toBe('get')
    expect(codeInput().getAttribute('name')).toBe('code')
  })

  it('RoomCode_Placeholder_IsSixDotsNotAFakeCode', () => {
    renderHome()
    expect(codeInput().placeholder).toBe('······')
    expect(codeInput().value).toBe('')
  })

  it('RoomCode_TypingInvalidChars_FiltersAndUppercases', () => {
    renderHome()
    fireEvent.change(codeInput(), { target: { value: 'a7k-O1x!' } })
    expect(codeInput().value).toBe('A7KX')
  })

  it('RoomCode_LessThanSixChars_KeepsButtonDisabled', () => {
    renderHome()
    fireEvent.change(codeInput(), { target: { value: 'A7K29' } })
    expect((screen.getByRole('button', { name: 'Vào phòng' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('RoomCode_SixValidChars_EnablesButtonAndSubmitsToJoinWithCode', () => {
    renderHome()
    fireEvent.change(codeInput(), { target: { value: 'a7k29x' } })
    const button = screen.getByRole('button', { name: 'Vào phòng' }) as HTMLButtonElement
    expect(button.disabled).toBe(false)
    fireEvent.submit(codeInput().closest('form')!)
    expect(screen.getByTestId('location').textContent).toBe('/join?code=A7K29X')
  })

  it('RoomCode_SubmitWhenIncomplete_StaysOnHome', () => {
    renderHome()
    fireEvent.change(codeInput(), { target: { value: 'A7K' } })
    fireEvent.submit(codeInput().closest('form')!)
    expect(screen.queryByTestId('location')).toBeNull()
  })
})

describe('Điểm tính ngay', () => {
  it('ScoreLab_AnswerInstantly_Shows1000Points', () => {
    renderHome()
    fireEvent.change(screen.getByLabelText(/Trả lời sau/), { target: { value: '0' } })
    expect(screen.getByText('+1000')).toBeTruthy()
  })

  it('ScoreLab_ChooseWrong_ShowsZeroPointsAndYouStayThird', () => {
    renderHome()
    fireEvent.click(screen.getByRole('button', { name: 'Sai' }))
    expect(screen.getByText('+0')).toBeTruthy()
    expect(screen.getByText('3. Bạn')).toBeTruthy()
  })
})

describe('Cách chơi và chủ đề', () => {
  it('HowItWorks_MovingSlider_SwapsTheClause', () => {
    renderHome()
    expect(screen.getByRole('heading', { level: 2, name: 'Nhập chủ đề bạn muốn thi.' })).toBeTruthy()
    fireEvent.change(screen.getByRole('slider', { name: 'Chặng cách chơi' }), { target: { value: '2' } })
    expect(screen.getByRole('heading', { level: 2, name: 'Bạn duyệt rồi mới dùng.' })).toBeTruthy()
  })

  it('Showcase_UnknownTopic_ShowsPendingNoteInsteadOfFakeQuestions', () => {
    renderHome()
    fireEvent.change(screen.getByLabelText('Bạn muốn thi về chủ đề gì?'), { target: { value: 'Địa lý châu Âu' } })
    expect(screen.getByText(/AI sẽ soạn 5 đến 20 câu về/)).toBeTruthy()
    expect(screen.queryByText('Chiến thắng Bạch Đằng năm 938 do ai lãnh đạo?')).toBeNull()
  })

  it('Showcase_PresetButton_RefillsTheTopicInput', () => {
    renderHome()
    fireEvent.change(screen.getByLabelText('Bạn muốn thi về chủ đề gì?'), { target: { value: 'x' } })
    fireEvent.click(screen.getByRole('button', { name: 'Hệ Mặt Trời' }))
    expect((screen.getByLabelText('Bạn muốn thi về chủ đề gì?') as HTMLInputElement).value).toBe('Hệ Mặt Trời')
    expect(screen.getAllByText('Hành tinh nào gần Mặt Trời nhất?').length).toBeGreaterThan(0)
  })
})
