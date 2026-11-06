import { createContext } from 'react'
import type { GameConnectionContextValue } from './GameConnectionProvider'

// Tách khỏi GameConnectionProvider.tsx: file component chỉ nên export component (React Fast Refresh).
export const GameConnectionContext = createContext<GameConnectionContextValue | null>(null)
