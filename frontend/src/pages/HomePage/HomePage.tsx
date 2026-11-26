import { AppHeader } from '../../components/AppHeader'
import { useHealth } from '../../hooks/useHealth'
import { Hero } from './Hero'
import { HowItWorks } from './HowItWorks'
import { Showcase } from './Showcase'
import { useReveal } from './useReveal'

export function HomePage() {
  // Chỉ để đánh thức backend (host free ngủ sau 15 phút): không render kết quả, lỗi cũng không hiện.
  useHealth()
  const ref = useReveal<HTMLElement>()

  return (
    <>
      <AppHeader />
      <main ref={ref}>
        <Hero />
        <HowItWorks />
        <Showcase />
      </main>
    </>
  )
}
