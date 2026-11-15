import { Route, Routes } from 'react-router'
import { ProtectedRoute } from './components/ProtectedRoute'
import { DashboardPage } from './pages/DashboardPage/DashboardPage'
import { HomePage } from './pages/HomePage/HomePage'
import { JoinPage } from './pages/JoinPage/JoinPage'
import { LoginPage } from './pages/LoginPage/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage/NotFoundPage'
import { PlayPage } from './pages/PlayPage/PlayPage'
import { ProfilePage } from './pages/ProfilePage/ProfilePage'
import { RegisterPage } from './pages/RegisterPage/RegisterPage'
import { ShopPage } from './pages/ShopPage/ShopPage'
import { XpRankingPage } from './pages/XpRankingPage/XpRankingPage'
import { RoomLivePage } from './pages/RoomLivePage/RoomLivePage'
import { RoomPage } from './pages/RoomPage/RoomPage'

export function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/join" element={<JoinPage />} />
      <Route path="/play/:code" element={<PlayPage />} />
      <Route path="/ranking" element={<XpRankingPage />} />
      {/* Công khai: xem danh mục không cần đăng nhập; hành động mua yêu cầu đăng nhập ngay trong trang. */}
      <Route path="/shop" element={<ShopPage />} />
      <Route
        path="/profile"
        element={
          <ProtectedRoute>
            <ProfilePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/rooms/:code"
        element={
          <ProtectedRoute>
            <RoomPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/rooms/:code/live"
        element={
          <ProtectedRoute>
            <RoomLivePage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
