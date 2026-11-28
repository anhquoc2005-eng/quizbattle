import { Link } from 'react-router'
import { useAuth } from '../../hooks/useAuth'
import { JoinCodeForm } from './JoinCodeForm'
import { QuestionCard } from './QuestionCard'
import { SAMPLE_TOPICS } from './sampleTopics'

const HERO_QUESTION = SAMPLE_TOPICS[1]!.questions[0]!

/** Mỗi dòng chữ trồi lên từ sau đường chân chữ của chính nó (xem .home-mask trong index.css). */
function MaskedLine({ children, delay }: { children: string; delay: string }) {
  return (
    <span className="home-mask">
      <span style={{ '--d': delay } as React.CSSProperties}>{children}</span>
    </span>
  )
}

export function Hero() {
  const { user, isLoading } = useAuth()
  return (
    <section className="col grid gap-12 pb-[clamp(48px,min(14vw,14vh),200px)] pt-[clamp(24px,min(7vw,7vh),96px)] lg:grid-cols-[minmax(0,1fr)_minmax(0,400px)] lg:items-center">
      <div>
        <h1 className="text-[clamp(48px,min(8vw,13vh),120px)] font-semibold leading-[1.05] tracking-[-0.04em]">
          <MaskedLine delay="0s">Vào trận</MaskedLine>{' '}
          <MaskedLine delay="0.11s">bằng mã</MaskedLine>
        </h1>
        <div className="hero-rise mt-3" style={{ '--d': '0.7s' } as React.CSSProperties}>
          <JoinCodeForm />
        </div>
        <p className="hero-rise mt-10 max-w-[40ch] text-lg text-muted" style={{ '--d': '0.8s' } as React.CSSProperties}>
          Quiz trực tiếp cho lớp và nhóm bạn: AI soạn câu hỏi, cả phòng trả lời bằng điện thoại.
        </p>
        {!isLoading && (
          <div className="hero-actions hero-rise mt-6 flex flex-wrap gap-3.5" style={{ '--d': '0.9s' } as React.CSSProperties}>
            <Link className="btn btn-ghost h-11 px-6 text-base" to={user ? '/dashboard' : '/register'}>
              Tạo phòng
            </Link>
            {!user && (
              <Link className="btn btn-ghost h-11 px-6 text-base" to="/login">
                Đăng nhập
              </Link>
            )}
          </div>
        )}
      </div>
      <div className="hero-rise" style={{ '--d': '1s' } as React.CSSProperties} aria-hidden="true">
        <QuestionCard question={HERO_QUESTION} timer revealed score="+850" />
      </div>
    </section>
  )
}
