import { useState } from 'react'
import { DEMO_TIME_LIMIT_SECONDS, demoScore } from './scoreDemo'
import { StepRange } from './StepRange'

type Outcome = 'correct' | 'wrong'

const RIVALS = [
  { name: 'Minh', score: 1740 },
  { name: 'Lan', score: 1420 },
  { name: 'Khoa', score: 980 },
]
// Điểm của bạn ở 2 câu trước (ví dụ), cộng với câu đang chỉnh ở dưới.
const YOU_BEFORE = 1180

function Segmented<T extends string>({
  label,
  value,
  options,
  onChange,
}: {
  label: string
  value: T
  options: { value: T; text: string }[]
  onChange: (v: T) => void
}) {
  return (
    <div role="group" aria-label={label} className="flex gap-2">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          onClick={() => onChange(o.value)}
          aria-pressed={o.value === value}
          className={`min-h-11 flex-1 rounded-full px-5 text-base font-semibold ${o.value === value ? 'bg-ink text-ground' : 'bg-raised'}`}
        >
          {o.text}
        </button>
      ))}
    </div>
  )
}

/** Điều khiển thật: chỉnh giây trả lời và đúng/sai, điểm và bảng xếp hạng bên trái đổi ngay theo công thức của server. */
export function ScoreLab() {
  const [elapsed, setElapsed] = useState(4)
  const [outcome, setOutcome] = useState<Outcome>('correct')
  const points = demoScore(outcome === 'correct', elapsed)
  const board = [...RIVALS, { name: 'Bạn', score: YOU_BEFORE + points }].sort((a, b) => b.score - a.score)

  return (
    <section aria-labelledby="score-title" className="col pb-[clamp(96px,14vw,200px)]" data-reveal>
      <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:gap-16">
        <div className="lg:sticky lg:top-8 lg:self-start">
          <div data-ui className="space-y-4 rounded-card bg-surface p-5 text-base sm:p-8" aria-live="polite">
            <p className="text-muted">Câu mẫu: bạn trả lời sau {elapsed} giây</p>
            <p className="text-[clamp(48px,8vw,120px)] font-semibold leading-none tracking-[-0.04em] tabular-nums text-accent">
              +{points}
            </p>
            <ol className="space-y-2">
              {board.map((p, i) => (
                <li
                  key={p.name}
                  className={`flex items-center justify-between rounded-field px-3 py-3 ${p.name === 'Bạn' ? 'bg-ink text-ground font-semibold' : 'bg-raised'}`}
                >
                  <span>
                    {i + 1}. {p.name}
                  </span>
                  <span className="tabular-nums">{p.score}</span>
                </li>
              ))}
            </ol>
          </div>
        </div>
        <div className="space-y-12 lg:pt-8">
          <h2 id="score-title" className="text-[clamp(30px,4.6vw,64px)] font-semibold leading-[1.05] tracking-[-0.03em]">
            Điểm tính ngay
          </h2>
          <div className="space-y-4">
            <p className="text-2xl font-semibold">Nhanh hơn, điểm cao hơn.</p>
            <p className="max-w-[44ch] text-lg text-muted">
              Đúng được 500 điểm, cộng thêm tối đa 500 tùy số giây còn lại. Mỗi câu mặc định có {DEMO_TIME_LIMIT_SECONDS} giây. Kéo để thử.
            </p>
            <label htmlFor="score-elapsed" className="block text-base">
              Trả lời sau <span className="tabular-nums font-semibold">{elapsed}</span> giây
            </label>
            <StepRange id="score-elapsed" min={0} max={DEMO_TIME_LIMIT_SECONDS} value={elapsed} onChange={setElapsed} />
          </div>
          <div className="space-y-4">
            <p className="text-2xl font-semibold">Sai thì không có điểm.</p>
            <p className="max-w-[44ch] text-lg text-muted">
              Chọn sai đáp án hoặc quá giờ thì câu đó 0 điểm. Điểm do server tính, máy bạn không gửi được điểm lên.
            </p>
            <Segmented
              label="Kết quả câu trả lời"
              value={outcome}
              onChange={setOutcome}
              options={[
                { value: 'correct', text: 'Đúng' },
                { value: 'wrong', text: 'Sai' },
              ]}
            />
          </div>
          <div className="space-y-2">
            <p className="text-2xl font-semibold">Bảng điểm cập nhật sau mỗi câu.</p>
            <p className="max-w-[44ch] text-lg text-muted">
              Hết câu, cả phòng thấy thứ hạng mới. Thắng trận thì nhận XP và Xu để mua nhân vật ở cửa hàng.
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}
