import type { CreatePlayerResult } from '../types/game'
import type { Room } from '../types/room'
import { request } from './client'

export function createPlayer(code: string, input: { displayName: string }): Promise<CreatePlayerResult> {
  return request<CreatePlayerResult>(`/api/rooms/${code}/players`, { method: 'POST', body: JSON.stringify(input) })
}

export function openRoom(code: string): Promise<Room> {
  return request<Room>(`/api/rooms/${code}/open`, { method: 'POST' })
}
