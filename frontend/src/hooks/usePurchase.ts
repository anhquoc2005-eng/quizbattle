import { useMutation, useQueryClient } from '@tanstack/react-query'
import { purchaseItem } from '../api/shop'

/**
 * Mua một món. Dù thành công hay lỗi đều làm mới ['profile'] (số dư và món đã sở hữu do server quyết, client không tự cập nhật bản sao).
 * Lỗi "already_owned" (ví dụ bấm hai lần) sau khi làm mới thành ra đúng trạng thái "đã sở hữu".
 */
export function usePurchase() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemKey: string) => purchaseItem(itemKey),
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['profile'] }),
  })
}
