import { useContext } from 'react'
import { GameConnectionContext } from './GameConnectionProvider'

export function useGameConnection() {
  const context = useContext(GameConnectionContext)
  if (!context) {
    throw new Error('useGameConnection phải được dùng bên trong GameConnectionProvider')
  }
  return context
}
