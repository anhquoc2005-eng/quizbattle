import { useQuery } from '@tanstack/react-query'
import { getProfile, getRewards } from '../api/profile'

export function useProfile() {
  return useQuery({ queryKey: ['profile'], queryFn: getProfile })
}

export function useRewards() {
  return useQuery({ queryKey: ['profile', 'rewards'], queryFn: () => getRewards() })
}
