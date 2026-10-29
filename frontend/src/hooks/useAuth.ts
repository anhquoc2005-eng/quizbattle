import { useContext } from 'react'
import { AuthContext } from '../components/AuthProvider'

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth phải được dùng bên trong AuthProvider')
  }
  return context
}
