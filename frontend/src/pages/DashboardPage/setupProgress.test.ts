import { describe, expect, it } from 'vitest'
import { getSetupProgress } from './setupProgress'

describe('getSetupProgress', () => {
  it('đang tải: bước tạo phòng ở trạng thái loading, không khẳng định chưa/đã xong', () => {
    expect(getSetupProgress(true, false)).toEqual({ doneSteps: 1, createRoomStatus: 'loading' })
  })

  it('đã tải, chưa có phòng: bước tạo phòng todo', () => {
    expect(getSetupProgress(false, false)).toEqual({ doneSteps: 1, createRoomStatus: 'todo' })
  })

  it('đã tải, có phòng: bước tạo phòng done', () => {
    expect(getSetupProgress(false, true)).toEqual({ doneSteps: 2, createRoomStatus: 'done' })
  })
})
