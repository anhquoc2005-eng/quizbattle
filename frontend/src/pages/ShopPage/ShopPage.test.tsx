// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import { getMyAvatar } from '../../api/avatar'
import { getProfile } from '../../api/profile'
import { getShopItems, purchaseItem } from '../../api/shop'
import { TEST_AVATAR } from '../../avatar/avatarFixture'
import { peekAvatarDataUri, renderAvatarDataUri } from '../../avatar/avatarRenderer'
import { useAuth } from '../../hooks/useAuth'
import type { AvatarItem, AvatarSlot, AvatarTier, MyAvatar } from '../../types/avatar'
import type { Profile } from '../../types/profile'
import { ShopPage } from './ShopPage'

vi.mock('../../api/shop', () => ({ getShopItems: vi.fn(), purchaseItem: vi.fn() }))
vi.mock('../../api/avatar', () => ({ getMyAvatar: vi.fn(), saveMyAvatar: vi.fn() }))
vi.mock('../../api/profile', () => ({ getProfile: vi.fn(), getRewards: vi.fn() }))
vi.mock('../../hooks/useAuth', () => ({ useAuth: vi.fn() }))
vi.mock('../../avatar/avatarRenderer', () => ({ peekAvatarDataUri: vi.fn(), renderAvatarDataUri: vi.fn() }))

const item = (slot: AvatarSlot, variantKey: string, name: string, tier: AvatarTier, price: number): AvatarItem => ({
  key: `${slot}.${variantKey}`, slot, variantKey, name, tier, price,
})

const ITEMS: AvatarItem[] = [
  item('hair', 'variant01', 'Tóc 01', 'free', 0), // món miễn phí: không có trong cửa hàng
  item('hair', 'variant03', 'Tóc 03', 'common', 50),
  item('gesture', 'ok', 'OK', 'rare', 150),
  item('clothes', 'variant11', 'Áo 11', 'common', 50),
  item('clothesGraphic', 'galaxy', 'Thiên hà', 'special', 400),
]

const PROFILE: Profile = { userId: 'u1', displayName: 'Alice', level: 3, totalXp: 640, xpIntoLevel: 40, xpForNextLevel: 600, coinBalance: 500 }
const mine = (owned: string[] = ['hair.variant03'], avatar = TEST_AVATAR): MyAvatar => ({ avatar, isDefault: false, ownedItemKeys: owned })

function signIn(isSignedIn: boolean, isLoading = false) {
  vi.mocked(useAuth).mockReturnValue({ user: isSignedIn ? { id: 'u1', email: 'a@x.com', displayName: 'Alice' } : null, isLoading } as ReturnType<typeof useAuth>)
}

function renderPage() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <ShopPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const section = (name: string) => within(screen.getByRole('region', { name }))
const card = (name: string) => within(screen.getByText(name).closest('li')!)

beforeEach(() => {
  signIn(true)
  vi.mocked(getShopItems).mockReset().mockResolvedValue(ITEMS)
  vi.mocked(getMyAvatar).mockReset().mockResolvedValue(mine())
  vi.mocked(getProfile).mockReset().mockResolvedValue(PROFILE)
  vi.mocked(purchaseItem).mockReset().mockResolvedValue({ itemKey: 'gesture.ok', price: 150, coinBalance: 350 })
  vi.mocked(peekAvatarDataUri).mockReset().mockReturnValue(undefined)
  vi.mocked(renderAvatarDataUri).mockReset().mockImplementation(() => Promise.resolve('data:image/svg+xml,x'))
})
afterEach(cleanup)

describe('ShopPage: ba trạng thái', () => {
  it('Loading_CatalogPending', () => {
    vi.mocked(getShopItems).mockReturnValue(new Promise(() => undefined))

    renderPage()

    expect(screen.getByText('Đang tải…')).not.toBeNull()
  })

  it('Loading_WhileTheSessionIsBeingRestored', () => {
    signIn(false, true)

    renderPage()

    expect(screen.getByText('Đang tải…')).not.toBeNull()
  })

  it('Error_ShowsMessageAndRetryReloadsTheCatalog', async () => {
    vi.mocked(getShopItems).mockRejectedValueOnce(new TypeError('Failed to fetch')).mockResolvedValueOnce(ITEMS)

    renderPage()

    expect(await screen.findByText('Không kết nối được máy chủ.')).not.toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByRole('region', { name: 'Tóc' })).not.toBeNull()
    expect(getShopItems).toHaveBeenCalledTimes(2)
  })

  it('Empty_NoItemsForSale', async () => {
    vi.mocked(getShopItems).mockResolvedValue([item('hair', 'variant01', 'Tóc 01', 'free', 0)])

    renderPage()

    expect(await screen.findByText('Cửa hàng chưa có món nào để bán.')).not.toBeNull()
  })
})

describe('ShopPage: danh sách', () => {
  it('ListsOnlyItemsForSale_GroupedBySlot_WithTierAndPrice', async () => {
    renderPage()
    await screen.findByRole('region', { name: 'Tóc' })

    expect(section('Tóc').getByText('Tóc 03')).not.toBeNull()
    expect(screen.queryByText('Tóc 01')).toBeNull() // miễn phí: không bán
    expect(card('Tóc 03').getByText('Thường · 50 Xu')).not.toBeNull()
    expect(card('OK').getByText('Hiếm · 150 Xu')).not.toBeNull()
    expect(card('Thiên hà').getByText('Đặc biệt · 400 Xu')).not.toBeNull()
    expect(screen.getAllByRole('region').map((r) => r.getAttribute('aria-label'))).toEqual(['Tóc', 'Áo', 'Cử chỉ', 'Hình in'])
  })

  it('MarksOwnedItems_WithoutABuyButton', async () => {
    renderPage()
    await screen.findByRole('region', { name: 'Tóc' })

    expect(card('Tóc 03').getByText('Đã sở hữu')).not.toBeNull()
    expect(card('Tóc 03').queryByRole('button', { name: /^Mua/ })).toBeNull()
    expect(card('OK').getByRole('button', { name: /^Mua/ })).not.toBeNull()
  })

  it('ShowsTheCoinBalanceWhenSignedIn', async () => {
    renderPage()

    expect(await screen.findByText('500 Xu')).not.toBeNull()
  })
})

describe('ShopPage: khách', () => {
  beforeEach(() => signIn(false))

  it('SeesCatalogAndPrices_NoBalance_LoginLinkInsteadOfBuyButtons', async () => {
    renderPage()
    await screen.findByRole('region', { name: 'Tóc' })

    expect(screen.queryByText(/ Xu$/, { selector: '[data-balance]' })).toBeNull()
    expect(screen.queryByRole('button', { name: /^Mua/ })).toBeNull()
    expect(screen.getAllByRole('link', { name: 'Đăng nhập để mua' }).length).toBe(4)
    expect(card('Tóc 03').getByText('Thường · 50 Xu')).not.toBeNull()
  })

  it('NeverCallsThePrivateEndpoints', async () => {
    renderPage()
    await screen.findByRole('region', { name: 'Tóc' })

    expect(getMyAvatar).not.toHaveBeenCalled()
    expect(getProfile).not.toHaveBeenCalled()
    expect(purchaseItem).not.toHaveBeenCalled()
  })

  it('CanPreviewAnyItemOnADefaultAvatarOfFreeItemsOnly', async () => {
    renderPage()
    await screen.findByRole('region', { name: 'Tóc' })

    fireEvent.click(card('Tóc 03').getByRole('button', { name: 'Xem thử' }))

    await waitFor(() =>
      expect(renderAvatarDataUri).toHaveBeenCalledWith(expect.objectContaining({ hair: 'variant03', eyes: 'variant01', background: 'd4d4d4', gesture: null })),
    )
  })
})

describe('ShopPage: mua đồ', () => {
  it('BuyPostsOnlyTheItemKey_OnceEvenWhenDoubleClicked_AndDisablesWhileSending', async () => {
    let resolve: (v: { itemKey: string; price: number; coinBalance: number }) => void = () => undefined
    vi.mocked(purchaseItem).mockReturnValue(new Promise((r) => (resolve = r)))
    renderPage()
    await screen.findByRole('region', { name: 'Cử chỉ' })

    const buy = card('OK').getByRole('button', { name: /^Mua/ })
    fireEvent.click(buy)
    fireEvent.click(buy)

    await waitFor(() => expect(purchaseItem).toHaveBeenCalledTimes(1)) // mutate gọi mutationFn bất đồng bộ
    expect(purchaseItem).toHaveBeenCalledWith('gesture.ok')
    await waitFor(() => expect((card('OK').getByRole('button', { name: /Đang mua/ }) as HTMLButtonElement).disabled).toBe(true))
    await new Promise((r) => setTimeout(r, 30))
    expect(purchaseItem).toHaveBeenCalledTimes(1) // vẫn đúng một lần sau khi mọi thứ lắng xuống
    resolve({ itemKey: 'gesture.ok', price: 150, coinBalance: 350 })
    await waitFor(() => expect(screen.queryByRole('button', { name: /Đang mua/ })).toBeNull())
  })

  it('SuccessRefreshesBalanceAndOwnership', async () => {
    renderPage()
    await screen.findByText('500 Xu')
    vi.mocked(getProfile).mockResolvedValue({ ...PROFILE, coinBalance: 350 })
    vi.mocked(getMyAvatar).mockResolvedValue(mine(['hair.variant03', 'gesture.ok']))

    fireEvent.click(card('OK').getByRole('button', { name: /^Mua/ }))

    expect(await screen.findByText('350 Xu')).not.toBeNull()
    await waitFor(() => expect(card('OK').getByText('Đã sở hữu')).not.toBeNull())
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('NotEnoughCoins_BuyButtonIsDisabledWithAReason', async () => {
    vi.mocked(getProfile).mockResolvedValue({ ...PROFILE, coinBalance: 100 })
    renderPage()
    await screen.findByText('100 Xu')

    const buy = card('OK').getByRole('button', { name: /Không đủ Xu/ }) as HTMLButtonElement
    expect(buy.disabled).toBe(true)
    expect((card('Áo 11').getByRole('button', { name: /^Mua/ }) as HTMLButtonElement).disabled).toBe(false) // 50 Xu mua được
  })

  it('AlreadyOwnedAfterADoubleClick_IsTreatedAsSuccess_RefreshesWithoutRedError', async () => {
    vi.mocked(purchaseItem).mockRejectedValue(new ApiError(409, { code: 'already_owned', title: 'Bạn đã sở hữu món này.' }))
    renderPage()
    await screen.findByText('500 Xu')

    fireEvent.click(card('OK').getByRole('button', { name: /^Mua/ }))

    await waitFor(() => expect(getProfile).toHaveBeenCalledTimes(2)) // làm mới
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('OtherServerErrors_ShowTheMappedMessage', async () => {
    vi.mocked(purchaseItem).mockRejectedValue(new ApiError(409, { code: 'insufficient_coins', title: 'x' }))
    renderPage()
    await screen.findByText('500 Xu')

    fireEvent.click(card('OK').getByRole('button', { name: /^Mua/ }))

    expect((await screen.findByRole('alert')).textContent).toContain('Bạn không đủ Xu để mua món này.')
  })
})

describe('ShopPage: xem thử', () => {
  it('SignedIn_PreviewsOnTheSavedAvatar_AndLeavesItUntouched', async () => {
    vi.mocked(getMyAvatar).mockResolvedValue(mine(['hair.variant03'], { ...TEST_AVATAR, eyes: 'variant02' }))
    renderPage()
    await screen.findByRole('region', { name: 'Cử chỉ' })

    fireEvent.click(card('OK').getByRole('button', { name: 'Xem thử' }))

    await waitFor(() => expect(renderAvatarDataUri).toHaveBeenCalledWith(expect.objectContaining({ gesture: 'ok', eyes: 'variant02' })))
    fireEvent.click(screen.getByRole('button', { name: 'Bỏ xem thử' }))
    await waitFor(() => expect(renderAvatarDataUri).toHaveBeenLastCalledWith(expect.objectContaining({ gesture: null, eyes: 'variant02' })))
    expect(purchaseItem).not.toHaveBeenCalled() // xem thử không tốn Xu
  })

  it('GraphicOnAnUnprintableShirt_SwitchesToPrintableShirtAndShowsANote', async () => {
    vi.mocked(getMyAvatar).mockResolvedValue(mine(['hair.variant03'], { ...TEST_AVATAR, clothes: 'variant14' }))
    renderPage()
    await screen.findByRole('region', { name: 'Hình in' })

    fireEvent.click(card('Thiên hà').getByRole('button', { name: 'Xem thử' }))

    await waitFor(() => expect(renderAvatarDataUri).toHaveBeenCalledWith(expect.objectContaining({ clothesGraphic: 'galaxy', clothes: 'variant01' })))
    expect(screen.getByText(/Đã đổi sang áo 01/)).not.toBeNull()
  })
})
