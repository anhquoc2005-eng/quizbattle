import type { GenerateQuestionsResult, Question, QuestionInput } from '../types/question'
import { request } from './client'

export function getQuestions(code: string): Promise<Question[]> {
  return request<Question[]>(`/api/rooms/${code}/questions`)
}

export function generateQuestions(code: string, count: number): Promise<GenerateQuestionsResult> {
  return request<GenerateQuestionsResult>(`/api/rooms/${code}/questions/generate`, {
    method: 'POST',
    body: JSON.stringify({ count }),
  })
}

export function addQuestion(code: string, input: QuestionInput): Promise<Question> {
  return request<Question>(`/api/rooms/${code}/questions`, { method: 'POST', body: JSON.stringify(input) })
}

export function updateQuestion(code: string, id: string, input: QuestionInput): Promise<Question> {
  return request<Question>(`/api/rooms/${code}/questions/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

export function approveQuestion(code: string, id: string): Promise<Question> {
  return request<Question>(`/api/rooms/${code}/questions/${id}/approve`, { method: 'POST' })
}

export function deleteQuestion(code: string, id: string): Promise<void> {
  return request<void>(`/api/rooms/${code}/questions/${id}`, { method: 'DELETE' })
}
