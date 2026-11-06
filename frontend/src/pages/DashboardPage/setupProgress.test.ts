import { describe, expect, it } from 'vitest'
import { getSetupProgress } from './setupProgress'

describe('getSetupProgress', () => {
  it('đang tải: bước tạo phòng ở trạng thái loading, không khẳng định chưa/đã xong', () => {
    expect(getSetupProgress('pending', false)).toEqual({ doneSteps: 1, createRoomStatus: 'loading' })
  })

  it('tải lỗi: bước tạo phòng ở trạng thái error, không khẳng định chưa/đã xong', () => {
    expect(getSetupProgress('error', false)).toEqual({ doneSteps: 1, createRoomStatus: 'error' })
  })

  it('đã tải, chưa có phòng: bước tạo phòng todo', () => {
    expect(getSetupProgress('success', false)).toEqual({ doneSteps: 1, createRoomStatus: 'todo' })
  })

  it('đã tải, có phòng: bước tạo phòng done', () => {
    expect(getSetupProgress('success', true)).toEqual({ doneSteps: 2, createRoomStatus: 'done' })
  })
})
