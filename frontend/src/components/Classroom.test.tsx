// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { peekAvatarDataUri } from '../avatar/avatarRenderer'
import type { PlayerPresence } from '../realtime/gameReducer'
import { Classroom } from './Classroom'

vi.mock('../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x') }))

const player = (id: string, name: string, extra: Partial<PlayerPresence> = {}): PlayerPresence => ({
  playerId: id, displayName: name, score: 0, level: null, avatar: TEST_AVATAR, online: true, ...extra,
})

const manyPlayers = (count: number) => Array.from({ length: count }, (_, i) => player(`id-${i}`, `Người ${i + 1}`))

beforeEach(() => vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue('data:image/svg+xml,x'))
afterEach(cleanup)

const names = (container: HTMLElement) => Array.from(container.querySelectorAll('.desk-name')).map((n) => n.textContent)

describe('Classroom', () => {
  it('Empty_ShowsEmptyMessageAndNoGrid', () => {
    const { container } = render(<Classroom players={[]} />)

    expect(screen.getByText('Chưa có ai vào phòng.')).not.toBeNull()
    expect(container.querySelector('.classroom')).toBeNull()
  })

  it('EveryPlayerGetsADeskWithDecorativeAvatarAndName', () => {
    const { container } = render(<Classroom players={[player('1', 'An'), player('2', 'Bình'), player('3', 'Chi')]} />)

    const desks = container.querySelectorAll('li.desk')
    expect(desks).toHaveLength(3)
    desks.forEach((desk) => {
      const img = desk.querySelector('img')!
      expect(img.getAttribute('aria-hidden')).toBe('true') // trang trí: tên đã nằm ngay dưới
      expect(img.compareDocumentPosition(desk.querySelector('.desk-name')!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    })
    expect(names(container)).toEqual(['An', 'Bình', 'Chi'])
  })

  it('KeepsInputOrder_EvenWhenScoresAreInReverse', () => {
    // Bàn không được sắp theo điểm: người điểm cao nhất vẫn ngồi cuối nếu vào phòng cuối.
    const players = [player('1', 'An', { score: 10 }), player('2', 'Bình', { score: 500 }), player('3', 'Chi', { score: 900 })]
    const { container } = render(<Classroom players={players} />)
    expect(names(container)).toEqual(['An', 'Bình', 'Chi'])

    const reversedScores = players.map((p, i) => ({ ...p, score: (3 - i) * 100 }))
    const { container: second } = render(<Classroom players={reversedScores} />)
    expect(names(second)).toEqual(['An', 'Bình', 'Chi'])
  })

  it('NewPlayerJoining_AppearsLast_WithoutMovingOthers', () => {
    const initial = [player('1', 'An'), player('2', 'Bình')]
    const { container, rerender } = render(<Classroom players={initial} />)

    rerender(<Classroom players={[...initial, player('3', 'Chi')]} />)

    expect(names(container)).toEqual(['An', 'Bình', 'Chi'])
  })

  it('OfflinePlayer_KeepsDeskInPlace_WithoutOnlineMarker', () => {
    const { container } = render(<Classroom players={[player('1', 'An'), player('2', 'Bình', { online: false }), player('3', 'Chi')]} />)

    expect(names(container)).toEqual(['An', 'Bình', 'Chi'])
    expect(screen.queryByText('(mất kết nối)')).toBeNull() // hiển thị online nằm ngoài phạm vi
  })

  it('LongName_IsTruncatedByCssAndKeepsFullNameInTitle', () => {
    const longName = 'Nguyễn Thị Hồng Ngọc Bích Thảo'
    const { container } = render(<Classroom players={[player('1', longName)]} />)

    const name = container.querySelector('.desk-name')!
    expect(name.getAttribute('title')).toBe(longName)
    expect(name.textContent).toBe(longName)
  })

  it.each([
    [1, 'roomy', 'size-24'],
    [12, 'roomy', 'size-24'],
    [13, 'medium', 'size-16'],
    [30, 'medium', 'size-16'],
    [31, 'dense', 'size-12'],
    [50, 'dense', 'size-12'],
  ])('%i players -> %s density with avatar class %s', (count, density, avatarClass) => {
    const { container } = render(<Classroom players={manyPlayers(count)} />)

    expect(container.querySelector('.classroom')!.getAttribute('data-density')).toBe(density)
    expect(container.querySelectorAll('li.desk')).toHaveLength(count)
    expect(container.querySelector('li.desk img')!.className).toContain(avatarClass)
  })
})
