import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getMyAvatar, saveMyAvatar } from '../api/avatar'
import type { Avatar } from '../types/avatar'

/**
 * Nhân vật của tôi + món đã sở hữu. Khóa nằm dưới tiền tố ['profile'] có sẵn nên làm mới ['profile'] (sau khi lưu nhân vật hoặc
 * mua đồ) làm mới luôn cả hồ sơ lẫn nhân vật. `enabled` = false khi chưa đăng nhập.
 */
export function useMyAvatar(enabled = true) {
  return useQuery({ queryKey: ['profile', 'avatar'], queryFn: getMyAvatar, enabled })
}

/** Lưu toàn bộ nhân vật; thành công thì làm mới hồ sơ và nhân vật (server là nguồn sự thật, không tự cập nhật bản sao ở client). */
export function useSaveAvatar() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (avatar: Avatar) => saveMyAvatar(avatar),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['profile'] }),
  })
}
