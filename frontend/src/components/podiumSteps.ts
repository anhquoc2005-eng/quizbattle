import type { LeaderboardEntry } from '../types/game'

/** Số bậc trên bục vinh danh. */
export const PODIUM_SIZE = 3

export interface PodiumPlace {
  rank: number
  entry: LeaderboardEntry
}

/**
 * Các bậc trên bục từ bảng xếp hạng cuối (hàm thuần). `rank` của bảng xếp hạng luôn duy nhất (server gán Rank = vị trí sau khi sắp
 * theo điểm rồi thời gian trả lời đúng), nên mỗi bậc đúng MỘT người và hai người cùng điểm vẫn đứng hai bậc khác nhau. Không nhóm,
 * không tự tính lại hạng. Trả theo thứ tự hạng 1, 2, 3; ít hơn 3 người thì chỉ có các bậc có người.
 */
export function podiumSteps(entries: readonly LeaderboardEntry[]): PodiumPlace[] {
  return entries
    .filter((entry) => entry.rank >= 1 && entry.rank <= PODIUM_SIZE)
    .sort((a, b) => a.rank - b.rank)
    .map((entry) => ({ rank: entry.rank, entry }))
}
