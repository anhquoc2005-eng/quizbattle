import type { Avatar, MyAvatar, SavedAvatar } from '../types/avatar'
import { request } from './client'

/** Nhân vật hiện tại (đã lưu hoặc tự sinh) và danh sách món bán đã sở hữu. */
export function getMyAvatar(): Promise<MyAvatar> {
  return request<MyAvatar>('/api/users/me/avatar')
}

/** Thay toàn bộ nhân vật: gửi đủ 11 ô (ô tùy chọn trống gửi null). Một lỗi thì server không lưu gì. */
export function saveMyAvatar(avatar: Avatar): Promise<SavedAvatar> {
  return request<SavedAvatar>('/api/users/me/avatar', { method: 'PUT', body: JSON.stringify(avatar) })
}
