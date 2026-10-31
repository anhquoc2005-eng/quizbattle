import { RoomCodeBoxes } from './RoomCodeBoxes'

/** Thẻ giải thích luồng chơi, dùng chung ở trang đăng nhập và đăng ký. Theo docs/design/login.html. */
export function HowItWorksCard() {
  return (
    <section className="card grid content-start gap-6" aria-labelledby="how-it-works">
      <h2 id="how-it-works">Một trận QuizBattle diễn ra thế nào</h2>
      <RoomCodeBoxes chars={['K', '7', 'Q', '2', 'M', 'X']} label="Ví dụ mã phòng K7Q2MX" />
      <ol className="grid list-none gap-4 p-0">
        <Step title="Bạn nhập chủ đề">
          Ví dụ "Java OOP", chọn 5–20 câu. AI soạn câu hỏi, bạn duyệt và sửa trước khi chơi.
        </Step>
        <Step title="Gửi mã phòng cho bạn bè">
          Mỗi phòng có một mã 6 ký tự như trên. Người chơi nhập mã và tên là vào, tối đa 50 người.
        </Step>
        <Step title="Trả lời nhanh, điểm cao">
          Mỗi câu có 20 giây. Đúng và nhanh được nhiều điểm hơn, bảng xếp hạng hiện sau từng câu.
        </Step>
      </ol>
    </section>
  )
}

function Step({ title, children }: { title: string; children: string }) {
  return (
    <li className="grid gap-1">
      <strong className="font-semibold">{title}</strong>
      <span className="text-muted">{children}</span>
    </li>
  )
}
