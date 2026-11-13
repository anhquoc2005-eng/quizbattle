import { useQuery } from '@tanstack/react-query'
import { getProfile, getRewards } from '../api/profile'

/** `enabled` = false khi chưa đăng nhập (trang công khai như /shop không được gọi /api/users/me). */
export function useProfile(enabled = true) {
  return useQuery({ queryKey: ['profile'], queryFn: getProfile, enabled })
}

export function useRewards() {
  return useQuery({ queryKey: ['profile', 'rewards'], queryFn: () => getRewards() })
}
