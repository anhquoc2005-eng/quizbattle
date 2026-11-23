import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createPracticeSession, getPracticeRooms, getPracticeSession, submitPracticeAnswer } from '../api/practice'
import { useAuth } from './useAuth'

/** Key gắn userId để cache của người trước không lọt sang người sau đăng nhập cùng trình duyệt. */
export function usePracticeRooms() {
  const userId = useAuth().user?.id
  return useQuery({ queryKey: ['practice', userId, 'rooms'], queryFn: getPracticeRooms, enabled: !!userId })
}

export function usePracticeSession(sessionId: string) {
  const userId = useAuth().user?.id
  return useQuery({
    queryKey: ['practice', userId, 'session', sessionId],
    queryFn: () => getPracticeSession(sessionId),
    enabled: !!userId,
    retry: false,
  })
}

export function useStartPractice() {
  return useMutation({ mutationFn: (roomCode: string) => createPracticeSession(roomCode) })
}

/**
 * Nộp đáp án. Mutation mặc định không retry: lỗi mạng thì người dùng bấm nộp lại, server idempotent.
 * Chỉ làm mới danh sách phòng (XP hôm nay) và ['profile'] (TotalXp); KHÔNG làm mới session để câu vừa nộp không bị thay giữa chừng.
 */
export function useSubmitPracticeAnswer(sessionId: string) {
  const queryClient = useQueryClient()
  const userId = useAuth().user?.id
  return useMutation({
    mutationFn: (vars: { questionId: string; selectedIndex: number }) =>
      submitPracticeAnswer(sessionId, vars.questionId, vars.selectedIndex),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['practice', userId, 'rooms'] })
      void queryClient.invalidateQueries({ queryKey: ['profile'] })
    },
  })
}
