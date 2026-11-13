import type { AvatarItem, PurchaseResult } from '../types/avatar'
import { request } from './client'

/** Danh mục món đồ, công khai (không cần đăng nhập). */
export function getShopItems(): Promise<AvatarItem[]> {
  return request<AvatarItem[]>('/api/shop/items')
}

/** Mua một món (cần đăng nhập). Chỉ gửi mã món; giá do server quyết định. */
export function purchaseItem(itemKey: string): Promise<PurchaseResult> {
  return request<PurchaseResult>('/api/shop/purchases', { method: 'POST', body: JSON.stringify({ itemKey }) })
}
