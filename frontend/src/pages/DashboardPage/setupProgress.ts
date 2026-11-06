export type StepStatus = 'done' | 'todo' | 'loading' | 'error'
export type RoomsLoadState = 'pending' | 'error' | 'success'

export interface SetupProgress {
  /** Số bước chắc chắn đã xong (tạo tài khoản luôn xong; bước tạo phòng chỉ tính khi đã biết có phòng). */
  doneSteps: number
  createRoomStatus: StepStatus
}

/** Khi danh sách phòng đang tải hoặc tải lỗi thì chưa biết người dùng đã có phòng hay chưa, nên không được coi là "chưa tạo". */
export function getSetupProgress(loadState: RoomsLoadState, hasAnyRoom: boolean): SetupProgress {
  if (loadState === 'pending') {
    return { doneSteps: 1, createRoomStatus: 'loading' }
  }
  if (loadState === 'error') {
    return { doneSteps: 1, createRoomStatus: 'error' }
  }
  return hasAnyRoom ? { doneSteps: 2, createRoomStatus: 'done' } : { doneSteps: 1, createRoomStatus: 'todo' }
}
