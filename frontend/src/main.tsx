import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { ApiError } from './api/apiError'
import { App } from './App'
import { AuthProvider } from './components/AuthProvider'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Lỗi 4xx (404, 409...) là chắc chắn, không retry để hiện ngay - chỉ thử lại khi lỗi mạng hoặc server 5xx.
      retry: (failureCount, error) =>
        failureCount < 2 && (!(error instanceof ApiError) || error.status >= 500),
    },
  },
})

const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Không tìm thấy phần tử #root trong index.html')
}

createRoot(rootElement).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
