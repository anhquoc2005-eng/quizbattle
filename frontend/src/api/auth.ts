import type { AuthResponse, AuthUser } from '../types/auth'
import { request } from './client'

export function register(input: { email: string; password: string; displayName: string }): Promise<AuthResponse> {
  return request<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(input) })
}

export function login(input: { email: string; password: string }): Promise<AuthResponse> {
  return request<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(input) })
}

export function getMe(): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/me')
}
