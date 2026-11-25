interface LocationState {
  from?: { pathname: string; search?: string }
}

function isLocationState(value: unknown): value is LocationState {
  return typeof value === 'object' && value !== null
}

export function getRedirectPath(state: unknown): string {
  if (isLocationState(state) && typeof state.from?.pathname === 'string') {
    const { pathname, search } = state.from
    // Chỉ nhận đường dẫn nội bộ: '//host' sẽ bị trình duyệt hiểu là URL ngoài (open redirect).
    if (pathname.startsWith('/') && !pathname.startsWith('//')) {
      // Giữ cả ?query: từ /join quay lại cần mã phòng (?code=...).
      return pathname + (typeof search === 'string' ? search : '')
    }
  }
  return '/dashboard'
}
