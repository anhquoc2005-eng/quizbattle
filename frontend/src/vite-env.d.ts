/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** URL backend, ví dụ http://localhost:5000. Không đặt secret nào ở đây. */
  readonly VITE_API_URL: string | undefined
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
