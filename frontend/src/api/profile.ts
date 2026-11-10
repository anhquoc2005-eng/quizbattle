import type { Profile, RewardHistoryItem } from '../types/profile'
import { request } from './client'

export function getProfile(): Promise<Profile> {
  return request<Profile>('/api/users/me')
}

export function getRewards(limit = 20): Promise<RewardHistoryItem[]> {
  return request<RewardHistoryItem[]>(`/api/users/me/rewards?limit=${limit}`)
}
