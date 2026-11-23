import type {
  PracticeAnswerResult,
  PracticeRoomsResponse,
  PracticeSession,
  PracticeSessionRef,
} from '../types/practice'
import { request } from './client'

export function getPracticeRooms(): Promise<PracticeRoomsResponse> {
  return request<PracticeRoomsResponse>('/api/practice/rooms')
}

/** 201 (lượt mới) và 200 (lượt đang mở) cùng dạng body: chỉ cần sessionId. */
export function createPracticeSession(roomCode: string): Promise<PracticeSessionRef> {
  return request<PracticeSessionRef>('/api/practice/sessions', {
    method: 'POST',
    body: JSON.stringify({ roomCode }),
  })
}

export function getPracticeSession(sessionId: string): Promise<PracticeSession> {
  return request<PracticeSession>(`/api/practice/sessions/${sessionId}`)
}

export function submitPracticeAnswer(sessionId: string, questionId: string, selectedIndex: number): Promise<PracticeAnswerResult> {
  return request<PracticeAnswerResult>(`/api/practice/sessions/${sessionId}/answers`, {
    method: 'POST',
    body: JSON.stringify({ questionId, selectedIndex }),
  })
}
