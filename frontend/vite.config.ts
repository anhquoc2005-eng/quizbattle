import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
  },
  test: {
    // Vitest chỉ dùng cho logic thuần (không test giao diện), nên chạy trong môi trường Node.
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
