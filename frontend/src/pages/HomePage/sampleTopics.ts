/** Câu hỏi MẪU để minh họa giao diện; câu thật do AI sinh khi chủ phòng tạo phòng. */
export interface SampleQuestion {
  text: string
  options: [string, string, string, string]
  correctIndex: number
}

export interface SampleTopic {
  name: string
  questions: SampleQuestion[]
}

export const SAMPLE_TOPICS: SampleTopic[] = [
  {
    name: 'Lịch sử Việt Nam',
    questions: [
      { text: 'Chiến thắng Bạch Đằng năm 938 do ai lãnh đạo?', options: ['Lê Hoàn', 'Ngô Quyền', 'Trần Hưng Đạo', 'Lý Thường Kiệt'], correctIndex: 1 },
      { text: 'Khởi nghĩa Hai Bà Trưng nổ ra vào năm nào?', options: ['Năm 40', 'Năm 248', 'Năm 542', 'Năm 938'], correctIndex: 0 },
      { text: 'Kinh đô của nhà Nguyễn đặt ở đâu?', options: ['Hà Nội', 'Hoa Lư', 'Huế', 'Cổ Loa'], correctIndex: 2 },
    ],
  },
  {
    name: 'Hệ Mặt Trời',
    questions: [
      { text: 'Hành tinh nào gần Mặt Trời nhất?', options: ['Sao Thủy', 'Sao Kim', 'Trái Đất', 'Sao Hỏa'], correctIndex: 0 },
      { text: 'Hành tinh nào lớn nhất Hệ Mặt Trời?', options: ['Sao Thổ', 'Sao Mộc', 'Sao Hải Vương', 'Trái Đất'], correctIndex: 1 },
      { text: 'Hành tinh nào nổi tiếng với vành đai?', options: ['Sao Hỏa', 'Sao Kim', 'Sao Thổ', 'Sao Thủy'], correctIndex: 2 },
    ],
  },
  {
    name: 'Bóng đá World Cup',
    questions: [
      { text: 'Đội nào vô địch World Cup 2022?', options: ['Pháp', 'Croatia', 'Brazil', 'Argentina'], correctIndex: 3 },
      { text: 'World Cup 2018 được tổ chức ở nước nào?', options: ['Nga', 'Qatar', 'Brazil', 'Đức'], correctIndex: 0 },
      { text: 'Nước nào đăng cai World Cup 2022?', options: ['Nga', 'Qatar', 'Mỹ', 'Nhật Bản'], correctIndex: 1 },
    ],
  },
]

export function findSampleTopic(input: string): SampleTopic | undefined {
  const key = input.trim().toLowerCase()
  return SAMPLE_TOPICS.find((t) => t.name.toLowerCase() === key)
}
