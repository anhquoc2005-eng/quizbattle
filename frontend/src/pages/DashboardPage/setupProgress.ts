export type StepStatus = 'done' | 'todo' | 'loading'

export interface SetupProgress {
  /** Số bước chắc chắn đã xong (tạo tài khoản luôn xong; bước tạo phòng chỉ tính khi đã biết có phòng). */
  doneSteps: number
  createRoomStatus: StepStatus
}

/** Lúc danh sách phòng đang tải chưa biết người dùng đã có phòng hay chưa, nên không được coi là "chưa tạo". */
export function getSetupProgress(isPending: boolean, hasAnyRoom: boolean): SetupProgress {
  if (isPending) {
    return { doneSteps: 1, createRoomStatus: 'loading' }
  }
  return hasAnyRoom ? { doneSteps: 2, createRoomStatus: 'done' } : { doneSteps: 1, createRoomStatus: 'todo' }
}
