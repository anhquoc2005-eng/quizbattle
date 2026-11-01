// Khớp rule validate của backend (QuestionRules/QuestionInput) để báo lỗi nhanh phía client.

const OPTION_COUNT = 4

export function validateQuestionText(text: string): string | undefined {
  if (!text.trim()) return 'Nội dung câu hỏi không được để trống.'
  if (text.length > 500) return 'Nội dung câu hỏi tối đa 500 ký tự.'
  return undefined
}

export function validateOption(option: string): string | undefined {
  if (!option.trim()) return 'Đáp án không được để trống.'
  if (option.length > 200) return 'Đáp án tối đa 200 ký tự.'
  return undefined
}

export function validateOptions(options: string[]): string | undefined {
  if (options.length !== OPTION_COUNT) return `Phải có đúng ${OPTION_COUNT} đáp án.`

  const firstInvalid = options.map(validateOption).find((error) => error !== undefined)
  if (firstInvalid) return firstInvalid

  const normalized = options.map((option) => option.trim().toLowerCase())
  if (new Set(normalized).size !== normalized.length) return 'Các đáp án không được trùng nhau.'

  return undefined
}

export function validateCorrectIndex(correctIndex: number | null): string | undefined {
  if (correctIndex === null || correctIndex < 0 || correctIndex >= OPTION_COUNT) return 'Chọn đáp án đúng.'
  return undefined
}

export function validateGenerateCount(count: number): string | undefined {
  if (!Number.isInteger(count) || count < 5 || count > 20) return 'Số câu phải từ 5 đến 20.'
  return undefined
}

export interface QuestionFormValues {
  text: string
  options: string[]
  correctIndex: number | null
}

export interface QuestionFormErrors {
  text?: string
  options?: string
  correctIndex?: string
}

export function validateQuestionForm(values: QuestionFormValues): QuestionFormErrors {
  return {
    text: validateQuestionText(values.text),
    options: validateOptions(values.options),
    correctIndex: validateCorrectIndex(values.correctIndex),
  }
}
