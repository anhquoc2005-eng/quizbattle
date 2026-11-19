import { useQuery } from '@tanstack/react-query'
import { getAiQuota } from '../api/aiQuota'

/** Số lượt AI còn lại hôm nay. Phải invalidate ['ai-quota'] sau mỗi lần generate (kể cả lỗi): server có thể đã trừ/hoàn lượt. */
export function useAiQuota() {
  return useQuery({ queryKey: ['ai-quota'], queryFn: getAiQuota })
}
