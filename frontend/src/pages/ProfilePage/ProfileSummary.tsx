import type { Profile } from '../../types/profile'

/** Cấp, thanh tiến trình tới cấp kế (dữ liệu nên dùng accent), tổng XP và số dư Xu. */
export function ProfileSummary({ profile }: { profile: Profile }) {
  const percent = Math.min(100, Math.round((profile.xpIntoLevel / profile.xpForNextLevel) * 100))

  return (
    <section className="card grid gap-4" aria-labelledby="profile-title">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h1 id="profile-title">{profile.displayName}</h1>
        <p className="tabular-nums">Cấp {profile.level}</p>
      </div>

      <div className="grid gap-1.5">
        <div
          className="h-2 overflow-hidden rounded-full bg-raised"
          role="progressbar"
          aria-label="Tiến trình tới cấp kế"
          aria-valuemin={0}
          aria-valuemax={profile.xpForNextLevel}
          aria-valuenow={profile.xpIntoLevel}
        >
          <div className="h-full bg-accent" style={{ width: `${percent}%` }} />
        </div>
        <p className="text-xs tabular-nums text-faint">
          {profile.xpIntoLevel} / {profile.xpForNextLevel} XP tới cấp {profile.level + 1}
        </p>
      </div>

      <dl className="grid grid-cols-2 gap-3">
        <div className="rounded-[14px] bg-raised p-3">
          <dt className="text-xs text-faint">Tổng XP</dt>
          <dd className="tabular-nums">{profile.totalXp}</dd>
        </div>
        <div className="rounded-[14px] bg-raised p-3">
          <dt className="text-xs text-faint">Xu</dt>
          <dd className="tabular-nums">{profile.coinBalance}</dd>
        </div>
      </dl>
    </section>
  )
}
