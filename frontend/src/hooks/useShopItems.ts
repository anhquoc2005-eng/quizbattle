import { useQuery } from '@tanstack/react-query'
import { getShopItems } from '../api/shop'

/** Danh mục món đồ (công khai). Chỉ đổi khi backend có migration mới nên cho giữ lâu. */
export function useShopItems() {
  return useQuery({ queryKey: ['shop', 'items'], queryFn: getShopItems, staleTime: 60 * 60 * 1000 })
}
