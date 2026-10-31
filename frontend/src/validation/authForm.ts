// Khớp rule validate của backend (QuizBattle.Application/Auth) để báo lỗi nhanh phía client.
// Server vẫn là nơi quyết định cuối cùng.

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validateEmail(email: string): string | undefined {
  if (!email.trim()) return 'Email không được để trống.'
  if (email.length > 256) return 'Email tối đa 256 ký tự.'
  if (!EMAIL_REGEX.test(email)) return 'Email không đúng định dạng.'
  return undefined
}

export function validateRegisterPassword(password: string): string | undefined {
  if (password.length < 8 || password.length > 128) return 'Mật khẩu 8–128 ký tự.'
  return undefined
}

export function validateLoginPassword(password: string): string | undefined {
  if (!password) return 'Mật khẩu không được để trống.'
  if (password.length > 128) return 'Mật khẩu tối đa 128 ký tự.'
  return undefined
}

export function validateDisplayName(displayName: string): string | undefined {
  const trimmed = displayName.trim()
  if (trimmed.length < 1 || trimmed.length > 50) return 'Tên hiển thị 1–50 ký tự.'
  return undefined
}

export interface LoginFormValues {
  email: string
  password: string
}

export interface LoginFormErrors {
  email?: string
  password?: string
}

export function validateLoginForm(values: LoginFormValues): LoginFormErrors {
  return {
    email: validateEmail(values.email),
    password: validateLoginPassword(values.password),
  }
}

export interface RegisterFormValues {
  displayName: string
  email: string
  password: string
}

export interface RegisterFormErrors {
  displayName?: string
  email?: string
  password?: string
}

export function validateRegisterForm(values: RegisterFormValues): RegisterFormErrors {
  return {
    displayName: validateDisplayName(values.displayName),
    email: validateEmail(values.email),
    password: validateRegisterPassword(values.password),
  }
}

export function hasFormErrors(errors: object): boolean {
  return Object.values(errors).some((message) => message !== undefined)
}

/** Map errors của ProblemDetails (400 validation) vào đúng field, so khớp không phân biệt hoa thường, bỏ qua key lạ. */
export function mapServerErrorsToFields<TField extends string>(
  errors: Record<string, string[]> | undefined,
  fields: readonly TField[],
): Partial<Record<TField, string>> {
  if (!errors) return {}

  const fieldByLowerKey = new Map(fields.map((field) => [field.toLowerCase(), field]))
  const result: Partial<Record<TField, string>> = {}

  for (const [key, messages] of Object.entries(errors)) {
    const field = fieldByLowerKey.get(key.toLowerCase())
    if (field && messages[0]) {
      result[field] = messages[0]
    }
  }

  return result
}
