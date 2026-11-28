import { useState } from 'react'
import { StageFragment } from './StageFragment'
import { StepRange } from './StepRange'

const STAGES = [
  { name: 'Nhập chủ đề', clause: 'Nhập chủ đề bạn muốn thi.', detail: 'Gõ một chủ đề, chọn số câu và thời gian mỗi câu (5 đến 120 giây, mặc định 20).' },
  { name: 'AI soạn câu', clause: 'AI soạn 5 đến 20 câu.', detail: 'Mỗi câu có 4 đáp án và đúng một đáp án đúng. Chủ đề càng cụ thể, câu hỏi càng sát ý bạn.' },
  { name: 'Bạn duyệt', clause: 'Bạn duyệt rồi mới dùng.', detail: 'Câu AI soạn luôn chờ duyệt. Bạn duyệt, sửa hoặc bỏ từng câu trước khi mở phòng.' },
  { name: 'Cả phòng thi', clause: 'Trả lời nhanh, điểm cao hơn.', detail: 'Cả phòng trả lời bằng điện thoại. Đúng và nhanh thì được tới 1000 điểm mỗi câu.' },
]

/** Một thanh trượt điều khiển cả câu bên trái lẫn mảnh giao diện bên phải. */
export function HowItWorks() {
  const [stage, setStage] = useState(0)
  const last = STAGES.length - 1
  return (
    <section aria-labelledby="how-title" className="col pb-[clamp(96px,14vw,200px)]" data-reveal>
      <div className="grid gap-2 md:grid-cols-2">
        <div className="order-2 flex flex-col justify-center gap-6 md:order-1 md:p-8">
          <h2
            id="how-title"
            key={stage}
            className="home-fade text-[clamp(30px,4.6vw,64px)] font-semibold leading-[1.05] tracking-[-0.03em]"
            aria-live="polite"
          >
            {STAGES[stage]!.clause}
          </h2>
          <p key={`d${stage}`} className="home-fade max-w-[34ch] text-lg">
            {STAGES[stage]!.detail}
          </p>
        </div>
        <div className="order-1 grid place-items-center rounded-card bg-surface p-4 sm:p-8 md:order-2">
          <div key={stage} className="home-fade w-full max-w-[460px]">
            <StageFragment stage={stage} />
          </div>
        </div>
      </div>
      <div className="mt-8">
        <StepRange min={0} max={last} value={stage} onChange={setStage} ariaLabel="Chặng cách chơi" valueText={STAGES[stage]!.name} />
        <ol className="mt-4 grid grid-cols-2 gap-2 text-base sm:grid-cols-4">
          {STAGES.map((s, i) => (
            <li key={s.name}>
              <button
                type="button"
                onClick={() => setStage(i)}
                aria-current={i === stage ? 'step' : undefined}
                className={`min-h-11 text-left ${i === stage ? 'text-ink' : 'text-muted'} hover:text-ink`}
              >
                {s.name}
              </button>
            </li>
          ))}
        </ol>
      </div>
    </section>
  )
}
