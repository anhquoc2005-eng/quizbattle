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

  describe('trạng thái giơ tay và ✓/✗', () => {
    const roster = [player('a', 'An'), player('b', 'Bình'), player('c', 'Chi')]
    const statusOf = (container: HTMLElement) => Array.from(container.querySelectorAll('li.desk')).map((d) => d.getAttribute('data-status'))
    const badges = (container: HTMLElement) => Array.from(container.querySelectorAll('.desk-badge')).map((b) => b.getAttribute('aria-label'))

    it('NoRound_AllDesksIdleWithoutBadges', () => {
      const { container } = render(<Classroom players={roster} />)

      expect(statusOf(container)).toEqual(['idle', 'idle', 'idle'])
      expect(badges(container)).toEqual([])
    })

    it('Running_OnlyAnsweredPlayersRaiseHand', () => {
      const round = { ended: false, answeredPlayerIds: ['b'], correctPlayerIds: null }
      const { container } = render(<Classroom players={roster} round={round} />)

      expect(statusOf(container)).toEqual(['idle', 'raised', 'idle'])
      expect(badges(container)).toEqual(['Đã trả lời'])
    })

    it('Ended_CorrectGetsTick_WrongAndNoAnswerGetCross', () => {
      const round = { ended: true, answeredPlayerIds: [], correctPlayerIds: ['a'] } // b sai, c không trả lời
      const { container } = render(<Classroom players={roster} round={round} />)

      expect(statusOf(container)).toEqual(['correct', 'wrong', 'wrong'])
      expect(badges(container)).toEqual(['Trả lời đúng', 'Trả lời sai hoặc không trả lời', 'Trả lời sai hoặc không trả lời'])
    })

    it('Ended_CorrectListUnknown_NoBadgesAtAll', () => {
      const round = { ended: true, answeredPlayerIds: [], correctPlayerIds: null }
      const { container } = render(<Classroom players={roster} round={round} />)

      expect(statusOf(container)).toEqual(['idle', 'idle', 'idle'])
      expect(badges(container)).toEqual([])
    })

    it('OfflinePlayerWhoAnsweredCorrectly_StillGetsTick', () => {
      const players = [player('a', 'An', { online: false }), player('b', 'Bình')]
      const { container } = render(<Classroom players={players} round={{ ended: true, answeredPlayerIds: [], correctPlayerIds: ['a'] }} />)

      expect(statusOf(container)).toEqual(['correct', 'wrong'])
    })

    it('Badge_IsAnOverlay_AvatarImageAndSizeUnchanged', () => {
      const idle = render(<Classroom players={[player('a', 'An')]} />)
      const idleImg = idle.container.querySelector('img')!
      const idleClass = idleImg.className
      const idleSrc = idleImg.getAttribute('src')
      idle.unmount()

      const { container } = render(<Classroom players={[player('a', 'An')]} round={{ ended: true, answeredPlayerIds: [], correctPlayerIds: ['a'] }} />)

      const img = container.querySelector('li.desk img')!
      expect(img.className).toBe(idleClass) // không đổi cỡ/khung nhân vật
      expect(img.getAttribute('src')).toBe(idleSrc) // không đổi ảnh DiceBear
      expect(container.querySelector('.desk-badge')).not.toBeNull()
      expect(container.querySelectorAll('li.desk img')).toHaveLength(1)
    })

    it('StatusChangeOfOnePlayer_RerendersOnlyThatDesk', () => {
      const players = Array.from({ length: 6 }, (_, i) => player(`p${i}`, `Người ${i}`))
      const { rerender } = render(<Classroom players={players} round={{ ended: false, answeredPlayerIds: [], correctPlayerIds: null }} />)
      vi.mocked(peekAvatarDataUri).mockClear() // Avatar gọi peek đúng một lần mỗi lần vẽ

      rerender(<Classroom players={players} round={{ ended: false, answeredPlayerIds: ['p3'], correctPlayerIds: null }} />)

      expect(peekAvatarDataUri).toHaveBeenCalledTimes(1) // chỉ bàn p3; 5 bàn còn lại được memo giữ nguyên
    })
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
