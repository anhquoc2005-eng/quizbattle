import { ApiError, parseProblemDetails } from './apiError'

const API_URL = import.meta.env.VITE_API_URL

if (!API_URL) {
  throw new Error('Thiếu biến môi trường VITE_API_URL. Sao chép frontend/.env.example thành frontend/.env.')
}

/**
 * Hàm gọi HTTP dùng chung. Mọi lời gọi backend phải đi qua đây
 * để xử lý URL gốc và lỗi theo một cách thống nhất.
 */
export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  // Chỉ gắn Content-Type khi có body, để GET không phải gửi thêm request preflight CORS.
  if (init?.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_URL}${path}`, { ...init, headers })

  const body: unknown = await readJson(response)

  if (!response.ok) {
    throw new ApiError(response.status, parseProblemDetails(body))
  }

  return body as T
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) {
    return null
  }

  try {
    return JSON.parse(text) as unknown
  } catch {
    return null
  }
}
