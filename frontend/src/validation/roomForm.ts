// Khớp rule validate của backend (CreateRoomRequest) để báo lỗi nhanh phía client. Server vẫn là nơi quyết định cuối cùng.

export function validateTopic(topic: string): string | undefined {
  if (!topic.trim()) return 'Chủ đề không được để trống.'
  if (topic.length > 200) return 'Chủ đề tối đa 200 ký tự.'
  return undefined
}

export function validateQuestionTimeLimitSeconds(seconds: number): string | undefined {
  if (!Number.isInteger(seconds) || seconds < 5 || seconds > 120) return 'Thời gian mỗi câu 5–120 giây.'
  return undefined
}

export interface CreateRoomFormValues {
  topic: string
  questionTimeLimitSeconds: number
}

export interface CreateRoomFormErrors {
  topic?: string
  questionTimeLimitSeconds?: string
}

export function validateCreateRoomForm(values: CreateRoomFormValues): CreateRoomFormErrors {
  return {
    topic: validateTopic(values.topic),
    questionTimeLimitSeconds: validateQuestionTimeLimitSeconds(values.questionTimeLimitSeconds),
  }
}
