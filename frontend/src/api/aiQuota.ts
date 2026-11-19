import type { AiQuota } from '../types/aiQuota'
import { request } from './client'

export function getAiQuota(): Promise<AiQuota> {
  return request<AiQuota>('/api/ai-quota')
}
