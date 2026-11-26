import { Podium } from '../../components/Podium'
import { GUEST_PREVIEW_AVATAR } from '../../avatar/guestPreview'
import type { LeaderboardEntry } from '../../types/game'

// Bục MẪU để minh họa (tên và điểm do ta đặt), dùng đúng component Podium của màn thi.
const SAMPLE_PODIUM: LeaderboardEntry[] = [
  { playerId: 'p1', displayName: 'Minh', score: 4120, rank: 1, level: null, avatar: { ...GUEST_PREVIEW_AVATAR, hair: 'variant05', background: 'f5c518' } },
  { playerId: 'p2', displayName: 'Lan', score: 3860, rank: 2, level: null, avatar: { ...GUEST_PREVIEW_AVATAR, hair: 'variant12', background: 'a9d3dc' } },
  { playerId: 'p3', displayName: 'Khoa', score: 3450, rank: 3, level: null, avatar: { ...GUEST_PREVIEW_AVATAR, hair: 'variant02', background: 'd4d4d4' } },
]

/** Con số thật của hệ thống (ScoreCalculator, RoomCodeGenerator, Room.RESULT_DISPLAY_SECONDS) viết thành một đoạn văn, cạnh bục vinh danh. */
export function Stats() {
  return (
    <section aria-labelledby="stats-title" className="col pb-[clamp(96px,14vw,200px)]">
      <h2 id="stats-title" className="sr-only">
        Con số
      </h2>
      <div className="grid items-center gap-10 lg:grid-cols-[3fr_2fr] lg:gap-16">
        <p data-reveal className="text-[clamp(24px,2.4vw,34px)] font-medium leading-[1.2] tracking-[-0.02em] text-muted">
          Đúng được <strong className="font-semibold text-ink">500&nbsp;điểm</strong>, nhanh thì thêm tới{' '}
          <strong className="font-semibold text-ink">500&nbsp;điểm</strong>. Mã phòng chỉ{' '}
          <strong className="font-semibold text-ink">6&nbsp;ký&nbsp;tự</strong>. Hiện đáp án{' '}
          <strong className="font-semibold text-ink">6&nbsp;giây</strong> rồi tự sang câu tiếp.
        </p>
        <div data-ui data-reveal className="rounded-card bg-surface p-5 sm:p-8">
          <p className="mb-4 text-base text-muted">Bục vinh danh cuối trận (ví dụ)</p>
          <Podium entries={SAMPLE_PODIUM} />
        </div>
      </div>
    </section>
  )
}
