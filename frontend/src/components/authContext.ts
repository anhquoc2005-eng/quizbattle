import { createContext } from 'react'
import type { AuthContextValue } from './AuthProvider'

// Tách khỏi AuthProvider.tsx: file component chỉ nên export component (React Fast Refresh).
export const AuthContext = createContext<AuthContextValue | null>(null)
