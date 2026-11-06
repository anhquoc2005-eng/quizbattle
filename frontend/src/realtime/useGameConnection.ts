import { useContext } from 'react'
import { GameConnectionContext } from './gameConnectionContext'

export function useGameConnection() {
  const context = useContext(GameConnectionContext)
  if (!context) {
    throw new Error('useGameConnection phải được dùng bên trong GameConnectionProvider')
  }
  return context
}
