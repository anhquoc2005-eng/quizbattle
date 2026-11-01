export type QuestionReviewStatus = 'Pending' | 'Approved'

export const QUESTION_REVIEW_STATUS_LABEL: Record<QuestionReviewStatus, string> = {
  Pending: 'Chờ duyệt',
  Approved: 'Đã duyệt',
}

/** Khớp với QuestionResponse ở QuizBattle.Application/Questions. */
export interface Question {
  id: string
  orderIndex: number
  text: string
  options: string[]
  correctIndex: number
  reviewStatus: QuestionReviewStatus
}

/** Khớp với QuestionInput ở QuizBattle.Application/Questions (dùng chung cho thêm tay và sửa). */
export interface QuestionInput {
  text: string
  options: string[]
  correctIndex: number
}

/** Khớp với GenerateQuestionsResponse ở QuizBattle.Application/Questions. */
export interface GenerateQuestionsResult {
  requested: number
  created: number
  questions: Question[]
}
