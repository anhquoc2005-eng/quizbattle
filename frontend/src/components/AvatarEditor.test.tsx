// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/apiError'
import { getMyAvatar, saveMyAvatar } from '../api/avatar'
import { getShopItems } from '../api/shop'
import { TEST_AVATAR } from '../avatar/avatarFixture'
import type { Avatar, AvatarItem, AvatarSlot, AvatarTier, MyAvatar } from '../types/avatar'
import { AvatarEditor } from './AvatarEditor'

vi.mock('../api/avatar', () => ({ getMyAvatar: vi.fn(), saveMyAvatar: vi.fn() }))
vi.mock('../api/shop', () => ({ getShopItems: vi.fn() }))
vi.mock('../avatar/avatarRenderer', () => ({
  peekAvatarDataUri: vi.fn().mockReturnValue('data:image/svg+xml,x'),
  renderAvatarDataUri: vi.fn().mockResolvedValue('data:image/svg+xml,x'),
}))

const item = (slot: AvatarSlot, variantKey: string, name: string, tier: AvatarTier = 'free'): AvatarItem => ({
  key: `${slot}.${variantKey}`, slot, variantKey, name, tier, price: tier === 'free' ? 0 : 50,
})

const ITEMS: AvatarItem[] = [
  item('eyes', 'variant01', 'Mắt 01'), item('eyes', 'variant02', 'Mắt 02'),
  item('eyebrows', 'variant01', 'Lông mày 01'),
  item('mouth', 'variant01', 'Miệng 01'),
  item('nose', 'variant01', 'Mũi 01'),
  item('hair', 'variant01', 'Tóc 01'), item('hair', 'variant03', 'Tóc 03', 'common'), item('hair', 'variant04', 'Tóc 04', 'common'),
  item('hair', 'variant62', 'Khăn trùm đầu 1'),
  item('clothes', 'variant01', 'Áo 01'), item('clothes', 'variant14', 'Áo 14'),
  item('glasses', 'variant01', 'Kính 01', 'common'),
  item('beard', 'variant01', 'Râu 01', 'common'),
  item('gesture', 'ok', 'OK', 'rare'),
  item('clothesGraphic', 'galaxy', 'Thiên hà', 'special'),
  item('background', 'd4d4d4', 'Xám mặc định'),
]

function mine(avatar: Avatar = TEST_AVATAR, owned: string[] = ['hair.variant04', 'glasses.variant01', 'clothesGraphic.galaxy']): MyAvatar {
  return { avatar, isDefault: false, ownedItemKeys: owned }
}

function renderEditor() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <AvatarEditor />
    </QueryClientProvider>,
  )
}

const group = (name: string) => within(screen.getByRole('group', { name }))
const saveButton = () => screen.getByRole('button', { name: /^(Lưu|Đang lưu)/ })

beforeEach(() => {
  vi.mocked(getMyAvatar).mockReset().mockResolvedValue(mine())
  vi.mocked(getShopItems).mockReset().mockResolvedValue(ITEMS)
  vi.mocked(saveMyAvatar).mockReset().mockImplementation((avatar) => Promise.resolve({ avatar, isDefault: false }))
})
afterEach(cleanup)

describe('AvatarEditor: ba trạng thái', () => {
  it('Loading_ShowsLoadingText', () => {
    vi.mocked(getMyAvatar).mockReturnValue(new Promise(() => undefined))

    renderEditor()

    expect(screen.getByText('Đang tải…')).not.toBeNull()
  })

  it('Error_ShowsMessageAndRetryReloads', async () => {
    vi.mocked(getMyAvatar).mockRejectedValueOnce(new TypeError('Failed to fetch'))

    renderEditor()

    expect(await screen.findByText('Không kết nối được máy chủ.')).not.toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByRole('group', { name: 'Tóc' })).not.toBeNull()
    expect(getMyAvatar).toHaveBeenCalledTimes(2)
  })

  it('Empty_EmptyCatalogShowsEmptyMessage', async () => {
    vi.mocked(getShopItems).mockResolvedValue([])

    renderEditor()

    expect(await screen.findByText('Chưa có món nào trong danh mục.')).not.toBeNull()
  })
})

describe('AvatarEditor: danh sách món', () => {
  it('ListsOnlyFreeAndOwnedItems_NeverUnownedPaidOnes', async () => {
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })

    const hair = group('Tóc')
    expect(hair.getByRole('button', { name: 'Tóc 01' })).not.toBeNull() // miễn phí
    expect(hair.getByRole('button', { name: 'Khăn trùm đầu 1' })).not.toBeNull() // miễn phí
    expect(hair.getByRole('button', { name: 'Tóc 04' })).not.toBeNull() // đã sở hữu
    expect(hair.queryByRole('button', { name: 'Tóc 03' })).toBeNull() // chưa mua: không hiện
    expect(group('Cử chỉ').queryByRole('button', { name: 'OK' })).toBeNull() // cử chỉ Hiếm chưa mua
  })

  it('OptionalSlotsHaveAClearButton_RequiredSlotsDoNot', async () => {
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })

    for (const name of ['Kính', 'Râu', 'Cử chỉ', 'Hình in', 'Màu nền']) {
      expect(group(name).getByRole('button', { name: 'Không' }), name).not.toBeNull()
    }
    for (const name of ['Mắt', 'Lông mày', 'Miệng', 'Mũi', 'Tóc', 'Áo']) {
      expect(group(name).queryByRole('button', { name: 'Không' }), name).toBeNull()
    }
  })

  it('MarksTheCurrentChoiceAsPressed', async () => {
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })

    expect(group('Tóc').getByRole('button', { name: 'Tóc 01' }).getAttribute('aria-pressed')).toBe('true')
    expect(group('Tóc').getByRole('button', { name: 'Tóc 04' }).getAttribute('aria-pressed')).toBe('false')
    expect(group('Kính').getByRole('button', { name: 'Không' }).getAttribute('aria-pressed')).toBe('true')
  })
})

describe('AvatarEditor: lưu', () => {
  it('SaveIsDisabledUntilSomethingChanges', async () => {
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })

    expect((saveButton() as HTMLButtonElement).disabled).toBe(true)
    fireEvent.click(group('Tóc').getByRole('button', { name: 'Tóc 04' }))
    expect((saveButton() as HTMLButtonElement).disabled).toBe(false)
  })

  it('SavesAllElevenSlotsOnce_AndDisablesWhileSending', async () => {
    let resolve: (v: { avatar: Avatar; isDefault: boolean }) => void = () => undefined
    vi.mocked(saveMyAvatar).mockReturnValue(new Promise((r) => (resolve = r)))
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })
    fireEvent.click(group('Tóc').getByRole('button', { name: 'Tóc 04' }))
    fireEvent.click(group('Kính').getByRole('button', { name: 'Kính 01' }))

    fireEvent.click(saveButton())
    fireEvent.click(saveButton()) // bấm lần hai trong lúc đang gửi

    await waitFor(() => expect((saveButton() as HTMLButtonElement).disabled).toBe(true))
    expect(screen.getByRole('button', { name: 'Đang lưu…' })).not.toBeNull()
    expect(saveMyAvatar).toHaveBeenCalledTimes(1)
    const sent = vi.mocked(saveMyAvatar).mock.calls[0]![0]
    expect(Object.keys(sent)).toHaveLength(11)
    expect(sent).toEqual({ ...TEST_AVATAR, hair: 'variant04', glasses: 'variant01' })
    resolve({ avatar: sent, isDefault: false })
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Đang lưu…' })).toBeNull())
  })

  it('AfterSave_ReloadsFromServer_AndNothingIsPendingAnymore', async () => {
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })
    fireEvent.click(group('Tóc').getByRole('button', { name: 'Tóc 04' }))
    vi.mocked(getMyAvatar).mockResolvedValue(mine({ ...TEST_AVATAR, hair: 'variant04' }))

    fireEvent.click(saveButton())

    await waitFor(() => expect(getMyAvatar).toHaveBeenCalledTimes(2)) // làm mới sau khi lưu
    await waitFor(() => expect((saveButton() as HTMLButtonElement).disabled).toBe(true))
    expect(group('Tóc').getByRole('button', { name: 'Tóc 04' }).getAttribute('aria-pressed')).toBe('true')
  })

  it('ServerError_ShowsMappedMessage_AndKeepsTheDraft', async () => {
    vi.mocked(saveMyAvatar).mockRejectedValue(new ApiError(409, { code: 'item_not_owned', title: 'x' }))
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })
    fireEvent.click(group('Tóc').getByRole('button', { name: 'Tóc 04' }))

    fireEvent.click(saveButton())

    expect((await screen.findByRole('alert')).textContent).toContain('Bạn chưa sở hữu một món đã chọn')
    expect(group('Tóc').getByRole('button', { name: 'Tóc 04' }).getAttribute('aria-pressed')).toBe('true')
    expect((saveButton() as HTMLButtonElement).disabled).toBe(false) // thử lại được
  })

  it('DiscardButton_RestoresTheSavedAvatar', async () => {
    renderEditor()
    await screen.findByRole('group', { name: 'Tóc' })
    fireEvent.click(group('Tóc').getByRole('button', { name: 'Tóc 04' }))

    fireEvent.click(screen.getByRole('button', { name: 'Bỏ thay đổi' }))

    expect(group('Tóc').getByRole('button', { name: 'Tóc 01' }).getAttribute('aria-pressed')).toBe('true')
    expect((saveButton() as HTMLButtonElement).disabled).toBe(true)
  })
})

describe('AvatarEditor: hình in chỉ dùng được với áo 01 đến 10', () => {
  it('GraphicOptionsDisabledWithReason_WhenClothesCannotBePrinted', async () => {
    vi.mocked(getMyAvatar).mockResolvedValue(mine({ ...TEST_AVATAR, clothes: 'variant14' }))
    renderEditor()
    await screen.findByRole('group', { name: 'Hình in' })

    expect((group('Hình in').getByRole('button', { name: 'Thiên hà' }) as HTMLButtonElement).disabled).toBe(true)
    expect(group('Hình in').getByText(/chỉ dùng được với áo 01 đến 10/)).not.toBeNull()
    expect((group('Hình in').getByRole('button', { name: 'Không' }) as HTMLButtonElement).disabled).toBe(false)
  })

  it('GraphicEnabledAgain_WhenAPrintableShirtIsChosen', async () => {
    vi.mocked(getMyAvatar).mockResolvedValue(mine({ ...TEST_AVATAR, clothes: 'variant14' }))
    renderEditor()
    await screen.findByRole('group', { name: 'Hình in' })

    fireEvent.click(group('Áo').getByRole('button', { name: 'Áo 01' }))

    expect((group('Hình in').getByRole('button', { name: 'Thiên hà' }) as HTMLButtonElement).disabled).toBe(false)
    expect(group('Hình in').queryByText(/chỉ dùng được với áo 01 đến 10/)).toBeNull()
  })

  it('GraphicStillSelected_OnUnprintableShirt_BlocksSaveWithMessage', async () => {
    vi.mocked(getMyAvatar).mockResolvedValue(mine({ ...TEST_AVATAR, clothesGraphic: 'galaxy' }))
    renderEditor()
    await screen.findByRole('group', { name: 'Áo' })

    fireEvent.click(group('Áo').getByRole('button', { name: 'Áo 14' }))

    expect((saveButton() as HTMLButtonElement).disabled).toBe(true)
    expect(screen.getByRole('alert').textContent).toContain('Áo đang chọn không in được hình')
    fireEvent.click(group('Hình in').getByRole('button', { name: 'Không' })) // bỏ hình in thì lưu được
    expect((saveButton() as HTMLButtonElement).disabled).toBe(false)
  })
})
