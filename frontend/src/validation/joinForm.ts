import { validateDisplayName } from './authForm'

const ROOM_CODE_LENGTH = 6

export function validateRoomCode(code: string): string | undefined {
  if (code.trim().length !== ROOM_CODE_LENGTH) return `Mã phòng gồm ${ROOM_CODE_LENGTH} ký tự.`
  return undefined
}

export interface JoinFormValues {
  code: string
  displayName: string
}

export interface JoinFormErrors {
  code?: string
  displayName?: string
}

export function validateJoinForm(values: JoinFormValues): JoinFormErrors {
  return {
    code: validateRoomCode(values.code),
    displayName: validateDisplayName(values.displayName),
  }
}
