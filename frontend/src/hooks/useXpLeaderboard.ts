import { useQuery } from '@tanstack/react-query'
import { getXpLeaderboard } from '../api/leaderboards'

export function useXpLeaderboard() {
  return useQuery({ queryKey: ['leaderboards', 'xp'], queryFn: getXpLeaderboard })
}
