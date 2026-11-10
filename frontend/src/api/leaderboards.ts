import type { XpLeaderboardEntry } from '../types/profile'
import { request } from './client'

export function getXpLeaderboard(): Promise<XpLeaderboardEntry[]> {
  return request<XpLeaderboardEntry[]>('/api/leaderboards/xp')
}
