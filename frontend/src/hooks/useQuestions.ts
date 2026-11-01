import { useQuery } from '@tanstack/react-query'
import { getQuestions } from '../api/questions'

export function useQuestions(code: string) {
  const normalizedCode = code.toUpperCase()
  return useQuery({ queryKey: ['rooms', normalizedCode, 'questions'], queryFn: () => getQuestions(normalizedCode) })
}
