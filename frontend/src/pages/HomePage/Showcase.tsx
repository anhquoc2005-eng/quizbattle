import { useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../../hooks/useAuth'
import { QuestionCard } from './QuestionCard'
import { SAMPLE_TOPICS, findSampleTopic } from './sampleTopics'

/** Tiêu đề là ô nhập. Chủ đề có sẵn thì hiện câu mẫu; chủ đề khác chỉ báo AI sẽ soạn khi tạo phòng (không giả kết quả AI). */
export function Showcase() {
  const { user } = useAuth()
  const [topic, setTopic] = useState(SAMPLE_TOPICS[0]!.name)
  const sample = findSampleTopic(topic)
  const shown = topic.trim()

  return (
    <section aria-labelledby="showcase-title" className="col pb-[clamp(96px,14vw,200px)]" data-reveal>
      <h2 id="showcase-title" className="sr-only">
        Thử một chủ đề
      </h2>
      <label htmlFor="showcase-topic" className="text-lg text-muted">
        Bạn muốn thi về chủ đề gì?
      </label>
      <input
        id="showcase-topic"
        value={topic}
        onChange={(e) => setTopic(e.target.value)}
        maxLength={60}
        autoComplete="off"
        spellCheck={false}
        className="mt-2 block w-full min-w-0 bg-transparent py-2 text-[clamp(36px,8vw,120px)] font-semibold leading-[1.1] tracking-[-0.04em] outline-none focus-visible:outline-2 focus-visible:outline-ink"
      />
      <div className="mt-8 grid gap-6 lg:grid-cols-[minmax(0,560px)_1fr]">
        <div className="space-y-3">
          {sample ? (
            sample.questions.slice(0, 2).map((q) => <QuestionCard key={q.text} question={q} revealed />)
          ) : (
            <div data-ui className="space-y-3 rounded-card bg-surface p-5 text-base">
              <p className="text-muted">Chủ đề của bạn</p>
              <p className="text-lg font-semibold">{shown || 'Chưa nhập chủ đề'}</p>
              <p>
                {shown
                  ? `AI sẽ soạn 5 đến 20 câu về “${shown}” khi bạn tạo phòng. Câu nào bạn duyệt mới được dùng.`
                  : 'Nhập một chủ đề để xem câu hỏi sẽ nằm ở đâu.'}
              </p>
            </div>
          )}
        </div>
        <div className="flex flex-col gap-3 lg:pl-8">
          <p className="text-base text-muted">Thử chủ đề có sẵn (câu mẫu, không phải do AI sinh lúc này):</p>
          <div className="flex flex-wrap gap-2">
            {SAMPLE_TOPICS.map((t) => (
              <button
                key={t.name}
                type="button"
                onClick={() => setTopic(t.name)}
                aria-pressed={t.name === topic}
                className={`min-h-11 rounded-full px-5 text-base ${t.name === topic ? 'bg-ink text-ground' : 'bg-raised'}`}
              >
                {t.name}
              </button>
            ))}
          </div>
          <Link className="btn btn-ghost mt-2 h-11 w-fit px-6 text-base" to={user ? '/dashboard' : '/register'}>
            Tạo phòng với chủ đề này
          </Link>
        </div>
      </div>
    </section>
  )
}
