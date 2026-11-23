import { Link } from 'react-router'
import { LogoMark } from '../../components/LogoMark'

export function PracticeHeader() {
  return (
    <header className="col bar">
      <Link className="mark" to="/">
        <LogoMark />
        QuizBattle
      </Link>
      <nav>
        <Link to="/dashboard">Phòng của tôi</Link>
        <Link to="/practice">Ôn tập</Link>
      </nav>
    </header>
  )
}
