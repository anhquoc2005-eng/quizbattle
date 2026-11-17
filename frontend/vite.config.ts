import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
  },
  test: {
    // Mặc định môi trường Node cho logic thuần; test component khai báo `// @vitest-environment jsdom` ở đầu file.
    environment: 'node',
    include: ['src/**/*.test.ts', 'src/**/*.test.tsx'],
    // Vitest mặc định thay mọi file CSS bằng chuỗi rỗng; reducedMotion.test.ts cần đọc chữ của index.css (`?raw`) để kiểm tra khối
    // prefers-reduced-motion. Chỉ mở đúng import `?raw` này, các CSS khác vẫn bị bỏ qua.
    css: { include: [/index\.css\?raw/] },
    // api/client.ts ném lỗi khi thiếu VITE_API_URL; test component nạp nó gián tiếp qua AuthProvider.
    env: { VITE_API_URL: 'http://localhost:5000' },
  },
})
