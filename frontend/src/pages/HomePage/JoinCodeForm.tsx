import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { isCompleteRoomCode, sanitizeRoomCode } from '../../validation/roomCode'

/** Ô mã phòng nằm ngay dưới câu tiêu đề. Không có JS vẫn là form GET /join?code=... */
export function JoinCodeForm() {
  const [code, setCode] = useState('')
  const navigate = useNavigate()
  const complete = isCompleteRoomCode(code)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (complete) navigate(`/join?code=${code}`)
  }

  return (
    <form action="/join" method="get" onSubmit={handleSubmit} className="flex flex-wrap items-stretch gap-3">
      <label htmlFor="home-room-code" className="sr-only">
        Mã phòng
      </label>
      <input
        id="home-room-code"
        name="code"
        value={code}
        onChange={(e) => setCode(sanitizeRoomCode(e.target.value))}
        placeholder="······"
        autoComplete="off"
        autoCapitalize="characters"
        spellCheck={false}
        enterKeyHint="go"
        aria-describedby="home-room-code-hint"
        className="w-[4.7em] max-w-full rounded-[0.2em] bg-raised px-[0.2em] py-[0.08em] text-[clamp(40px,min(7.5vw,11vh),80px)] font-semibold uppercase leading-none tabular-nums tracking-[0.02em] text-accent caret-ink outline-none placeholder:text-faint focus-visible:outline-2 focus-visible:outline-ink"
      />
      <button type="submit" disabled={!complete} className="btn btn-primary h-auto! min-h-14 px-8 text-base">
        Vào phòng
      </button>
      <p id="home-room-code-hint" className="basis-full text-base text-muted">
        Mã gồm 6 chữ và số do chủ phòng đưa. Khách vào được, không cần tài khoản.
      </p>
    </form>
  )
}
