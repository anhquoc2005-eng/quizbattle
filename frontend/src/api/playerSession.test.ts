import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearPlayerSession, loadPlayerSession, savePlayerSession } from './playerSession'

// Môi trường test là Node thuần (không jsdom - xem vite.config.ts), không có localStorage sẵn.
// Polyfill tối thiểu bằng bộ nhớ trong tiến trình, đủ để test logic lưu/đọc/xóa và trường hợp ném lỗi.
class MemoryStorage implements Storage {
  private store = new Map<string, string>()

  get length() {
    return this.store.size
  }

  getItem(key: string): string | null {
    return this.store.has(key) ? this.store.get(key)! : null
  }

  setItem(key: string, value: string): void {
    this.store.set(key, value)
  }

  removeItem(key: string): void {
    this.store.delete(key)
  }

  clear(): void {
    this.store.clear()
  }

  key(index: number): string | null {
    return [...this.store.keys()][index] ?? null
  }
}

let memoryStorage: MemoryStorage

beforeEach(() => {
  memoryStorage = new MemoryStorage()
  vi.stubGlobal('localStorage', memoryStorage)
})

describe('playerSession', () => {
  it('lưu rồi đọc lại đúng phiên theo mã phòng', () => {
    savePlayerSession('ABCDEF', { playerId: 'p1', sessionToken: 'tok' })

    expect(loadPlayerSession('ABCDEF')).toEqual({ playerId: 'p1', sessionToken: 'tok' })
  })

  it('đọc phòng chưa từng lưu trả về null', () => {
    expect(loadPlayerSession('KHONGCO')).toBeNull()
  })

  it('mỗi mã phòng lưu riêng, không ghi đè lẫn nhau', () => {
    savePlayerSession('ABCDEF', { playerId: 'p1', sessionToken: 'tok1' })
    savePlayerSession('GHIJKL', { playerId: 'p2', sessionToken: 'tok2' })

    expect(loadPlayerSession('ABCDEF')).toEqual({ playerId: 'p1', sessionToken: 'tok1' })
    expect(loadPlayerSession('GHIJKL')).toEqual({ playerId: 'p2', sessionToken: 'tok2' })
  })

  it('xóa đúng phiên theo mã phòng', () => {
    savePlayerSession('ABCDEF', { playerId: 'p1', sessionToken: 'tok' })

    clearPlayerSession('ABCDEF')

    expect(loadPlayerSession('ABCDEF')).toBeNull()
  })

  it('savePlayerSession không ném lỗi khi localStorage ném lỗi', () => {
    vi.spyOn(memoryStorage, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    expect(() => savePlayerSession('ABCDEF', { playerId: 'p1', sessionToken: 'tok' })).not.toThrow()
  })

  it('loadPlayerSession trả về null khi localStorage ném lỗi', () => {
    vi.spyOn(memoryStorage, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    expect(loadPlayerSession('ABCDEF')).toBeNull()
  })

  it('clearPlayerSession không ném lỗi khi localStorage ném lỗi', () => {
    vi.spyOn(memoryStorage, 'removeItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    expect(() => clearPlayerSession('ABCDEF')).not.toThrow()
  })
})
