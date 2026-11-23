/** Khớp với PracticeDtos ở QuizBattle.Application/Practice. */
export type PracticeXpReason = 'wrong' | 'already_today' | 'daily_cap' | 'daily_cap_partial'

/** Câu của lượt ôn. Không có correctIndex: chỉ biết đáp án sau khi nộp. */
export interface PracticeQuestion {
  questionId: string
  index: number
  text: string
  options: string[]
}

export interface PracticeResult {
  questionId: string
  isCorrect: boolean
  correctIndex: number
  xpAwarded: number
  /** null = nhận đủ XP. */
  xpReason: PracticeXpReason | null
}

export interface PracticeSummary {
  correctCount: number
  xpGainedInSession: number
  xpGainedToday: number
  remainingToday: number
  resetAt: string
}

export interface PracticeSession {
  sessionId: string
  topic: string
  total: number
  answeredCount: number
  completed: boolean
  current: PracticeQuestion | null
  lastResult: PracticeResult | null
  summary: PracticeSummary | null
}

export interface PracticeAnswerResult {
  isCorrect: boolean
  correctIndex: number
  xpAwarded: number
  xpReason: PracticeXpReason | null
  xpGainedToday: number
  remainingToday: number
  completed: boolean
}

export interface PracticeRoom {
  roomCode: string
  topic: string
  finishedAt: string
  questionCount: number
  role: 'player' | 'host'
  openSessionId: string | null
}

export interface PracticeRoomsResponse {
  xpPerCorrect: number
  dailyXpCap: number
  xpGainedToday: number
  remainingToday: number
  resetAt: string
  rooms: PracticeRoom[]
}

export interface PracticeSessionRef {
  sessionId: string
  total: number
}
