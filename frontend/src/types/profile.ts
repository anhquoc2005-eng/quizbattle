import type { Avatar } from './avatar'

/** Khớp ProfileDto. Tiến trình = xpIntoLevel / xpForNextLevel (server tính, client không lặp công thức). */
export interface Profile {
  userId: string
  displayName: string
  level: number
  totalXp: number
  xpIntoLevel: number
  xpForNextLevel: number
  coinBalance: number
}

/** Khớp RewardHistoryItemDto - tổng Xu của trận = coins + rankBonus. */
export interface RewardHistoryItem {
  roomCode: string
  topic: string
  playedAt: string
  score: number
  rewardRank: number
  xp: number
  coins: number
  rankBonus: number
}

/** Khớp XpLeaderboardEntryDto (không có email hay Id người dùng). */
export interface XpLeaderboardEntry {
  rank: number
  displayName: string
  level: number
  totalXp: number
  avatar: Avatar
}
