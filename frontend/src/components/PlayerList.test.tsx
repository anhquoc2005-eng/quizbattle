// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import { peekAvatarDataUri } from '../avatar/avatarRenderer'
import type { PlayerPresence } from '../realtime/gameReducer'
import { PlayerList } from './PlayerList'

vi.mock('../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x') }))

const player = (id: string, name: string, extra: Partial<PlayerPresence> = {}): PlayerPresence => ({
  playerId: id, displayName: name, score: 0, level: null, avatar: TEST_AVATAR, online: true, ...extra,
})

beforeEach(() => vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue('data:image/svg+xml,x'))
afterEach(cleanup)

describe('PlayerList', () => {
  it('Empty_ShowsEmptyMessage', () => {
    render(<PlayerList players={[]} />)

    expect(screen.getByText('Chưa có ai vào phòng.')).not.toBeNull()
  })

  it('EveryPlayerHasASmallAvatarNextToTheName', () => {
    const { container } = render(<PlayerList players={[player('1', 'An'), player('2', 'Bình'), player('3', 'Chi')]} />)

    const rows = container.querySelectorAll('li')
    expect(rows).toHaveLength(3)
    rows.forEach((row) => {
      const img = row.querySelector('img')
      expect(img).not.toBeNull()
      expect(img!.className).toContain('size-7') // cỡ nhỏ 28 px
      expect(img!.getAttribute('aria-hidden')).toBe('true') // trang trí: tên đã nằm ngay cạnh
    })
  })

  it('AvatarComesBeforeTheNameInEachRow', () => {
    const { container } = render(<PlayerList players={[player('1', 'An')]} />)

    const row = container.querySelector('li')!
    const img = row.querySelector('img')!
    const name = screen.getByText('An')
    expect(img.compareDocumentPosition(name) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('ShowsLevelAndOfflineMarkerAsBefore', () => {
    render(<PlayerList players={[player('1', 'An', { level: 3 }), player('2', 'Bình', { online: false })]} />)

    expect(screen.getByText('Cấp 3')).not.toBeNull()
    expect(screen.getByText('(mất kết nối)')).not.toBeNull()
  })
})
