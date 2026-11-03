# QuizBattle Constitution

`CLAUDE.md` là nguồn duy nhất của quy tắc chi tiết. File này chỉ nêu nguyên tắc cốt lõi.
Khi hai file mâu thuẫn, **`CLAUDE.md` thắng**.

## Nguyên tắc cốt lõi

1. **Server là nguồn sự thật.** Điểm, thời gian và đúng/sai chỉ tính ở server; client chỉ hiển thị. (CLAUDE.md: Real-time)
2. **Kiến trúc phân lớp.** Controller/Hub → Service → Repository, DI qua interface. Logic nghiệp vụ thuần nằm ở Domain và có unit test. (CLAUDE.md: Backend, Test)
3. **Bảo mật.** Secret không vào repo hay frontend, validate mọi input, rate limit endpoint tốn kém. (CLAUDE.md: Bảo mật)
4. **Quy trình tính năng.** Mỗi tính năng một nhánh `feat/...`, commit nhỏ theo Conventional Commits, build + test qua trước khi merge, và người làm giải thích được khi phỏng vấn. (CLAUDE.md: Quy trình, Git)
5. **Không thêm thư viện khi chưa hỏi.** (CLAUDE.md: Stack)

**Version**: 1.0.0 | **Ratified**: 2026-11-03
