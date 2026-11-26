const ROWS = [
  { q: 'Cần tài khoản không?', a: 'Chủ phòng cần đăng ký để tạo phòng. Người chơi chỉ cần mã phòng và một cái tên, không cần tài khoản.' },
  { q: 'AI soạn được bao nhiêu câu?', a: 'Từ 5 đến 20 câu mỗi lần, mỗi câu có 4 đáp án. Bạn nhập chủ đề, AI lo phần còn lại.' },
  { q: 'Câu AI soạn dùng ngay được không?', a: 'Chưa. Mọi câu AI soạn đều chờ bạn duyệt, sửa hoặc bỏ trước khi mở phòng.' },
  { q: 'Điểm được tính thế nào?', a: 'Đúng được 500 điểm, cộng tối đa 500 điểm tùy tốc độ. Sai hoặc quá giờ là 0. Server tính, không ai gửi điểm lên được.' },
  { q: 'Đang chơi mà rớt mạng thì sao?', a: 'Vào lại bằng phiên cũ là về đúng phòng, giữ nguyên điểm.' },
  { q: 'Xu dùng để làm gì?', a: 'Mua nhân vật ở cửa hàng. Xu và XP nhận được sau mỗi trận, cũng có thể ôn tập một mình để lấy thêm XP.' },
]

/** Câu hỏi lớn bên trái, trả lời bên phải, mọi câu mở sẵn (không accordion). */
export function Faq() {
  return (
    <section aria-labelledby="faq-title" className="col pb-[clamp(96px,14vw,200px)]">
      <h2 id="faq-title" data-reveal className="text-[clamp(24px,2.5vw,36px)] font-medium leading-[1.1] text-muted">
        Hỏi nhanh
      </h2>
      <dl className="mt-12 space-y-14 lg:space-y-20">
        {ROWS.map((row, i) => (
          <div
            key={row.q}
            data-reveal
            style={{ transitionDelay: `${(i % 3) * 70}ms` }}
            className="grid gap-3 lg:grid-cols-12 lg:gap-0"
          >
            <dt className="text-[clamp(24px,2.5vw,36px)] font-medium leading-[1.1] tracking-[-0.02em] lg:col-span-6">{row.q}</dt>
            <dd className="max-w-[44ch] text-lg leading-[1.55] lg:col-span-5 lg:col-start-8">{row.a}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}
