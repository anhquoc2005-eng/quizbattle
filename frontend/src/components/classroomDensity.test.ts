import { describe, expect, it } from 'vitest'
import { classroomDensity, DESK_AVATAR_SIZE, MEDIUM_MAX_PLAYERS, ROOMY_MAX_PLAYERS } from './classroomDensity'

describe('classroomDensity', () => {
  it('Boundaries_AtTwelveAndThirty', () => {
    expect(classroomDensity(0)).toBe('roomy')
    expect(classroomDensity(ROOMY_MAX_PLAYERS)).toBe('roomy')
    expect(classroomDensity(ROOMY_MAX_PLAYERS + 1)).toBe('medium')
    expect(classroomDensity(MEDIUM_MAX_PLAYERS)).toBe('medium')
    expect(classroomDensity(MEDIUM_MAX_PLAYERS + 1)).toBe('dense')
    expect(classroomDensity(50)).toBe('dense')
  })

  it('AvatarGetsSmallerAsTheRoomGetsCrowded', () => {
    expect(DESK_AVATAR_SIZE).toEqual({ roomy: 'md', medium: 'tile-md', dense: 'tile-sm' })
  })
})
