import type { CreateRoomInput, Room } from '../types/room'
import { request } from './client'

export function getRooms(): Promise<Room[]> {
  return request<Room[]>('/api/rooms')
}

export function getRoom(code: string): Promise<Room> {
  return request<Room>(`/api/rooms/${code}`)
}

export function createRoom(input: CreateRoomInput): Promise<Room> {
  return request<Room>('/api/rooms', { method: 'POST', body: JSON.stringify(input) })
}

export function deleteRoom(code: string): Promise<void> {
  return request<void>(`/api/rooms/${code}`, { method: 'DELETE' })
}
