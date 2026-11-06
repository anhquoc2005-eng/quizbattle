import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist', 'node_modules']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [js.configs.recommended, tseslint.configs.recommended, reactHooks.configs.flat.recommended, reactRefresh.configs.vite],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
    },
    rules: {
      // Luật kiểu React Compiler: các effect đồng bộ state từ props/timer (đồng hồ, phiên đăng nhập) là chủ đích;
      // chỉ cảnh báo, không refactor để chiều lint (xem báo cáo cleanup).
      'react-hooks/set-state-in-effect': 'warn',
    },
  },
])
