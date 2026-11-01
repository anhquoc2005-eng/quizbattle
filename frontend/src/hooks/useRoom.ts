import { useQuery } from '@tanstack/react-query'
import { getRoom } from '../api/rooms'

/** code chuẩn hóa hoa để /rooms/k7q2mx và /rooms/K7Q2MX dùng chung 1 cache, khớp cách backend so khớp mã. */
export function useRoom(code: string) {
  const normalizedCode = code.toUpperCase()
  return useQuery({ queryKey: ['rooms', normalizedCode], queryFn: () => getRoom(normalizedCode) })
}
