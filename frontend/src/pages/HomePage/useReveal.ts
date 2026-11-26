import { useEffect, useRef } from 'react'

/**
 * Các phần tử [data-reveal] trồi lên (transform + opacity) một lần khi vào màn hình.
 * Chỉ ẩn trước khi vào khi script đã chạy (data-js) và người dùng không tắt chuyển động: lỗi script thì nội dung vẫn hiện.
 */
export function useReveal<T extends HTMLElement>() {
  const ref = useRef<T>(null)
  useEffect(() => {
    const root = ref.current
    if (!root) return
    const targets = root.querySelectorAll<HTMLElement>('[data-reveal]')
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
    if (reduce || typeof IntersectionObserver === 'undefined') {
      targets.forEach((el) => (el.dataset.in = '1'))
      return
    }
    root.dataset.js = '1'
    const io = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue
          ;(entry.target as HTMLElement).dataset.in = '1'
          io.unobserve(entry.target)
        }
      },
      { threshold: 0.12 },
    )
    targets.forEach((el) => io.observe(el))
    return () => io.disconnect()
  }, [])
  return ref
}
