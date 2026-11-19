# Specification Quality Checklist: Giới hạn AI theo ngày

**Purpose**: Validate specification completeness and quality before implementation
**Created**: 2026-11-19
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] Chi tiết kỹ thuật chỉ nằm trong "Quyết định sản phẩm đã chốt" và "Đối chiếu với code thật" (ràng buộc do cố vấn đưa); scenario/requirement viết theo hành vi
- [x] Tập trung giá trị người dùng (thấy số lượt, hết lượt có lý do rõ) và bảo vệ chi phí AI
- [x] Mọi mục bắt buộc đã có

## Requirement Completeness

- [x] Không còn `[NEEDS CLARIFICATION]`; các điểm còn mở nằm ở "Điểm cần chốt còn lại" kèm đề xuất mặc định
- [x] Yêu cầu kiểm thử được (FR-001…FR-011 đều có task kiểm tra)
- [x] Tiêu chí thành công đo được (SC-001…SC-008)
- [x] Kịch bản chấp nhận và edge case đã nêu (đổi trần giữa ngày, nhiều tab, tiến trình chết)
- [x] Phạm vi rõ (mục "Ngoài phạm vi")
- [x] Phụ thuộc và giả định nêu rõ (Rủi ro đã chấp nhận, plan.md mục Migration)

## Feature Readiness

- [x] Mỗi quyết định Q1–Q7 và A–E có ít nhất một task (bảng đối chiếu cuối tasks.md)
- [x] Test Postgres thật bắt buộc: đồng thời user/global, xen kẽ, ranh giới ngày VN, hủy không hoàn, hoàn khi Gemini lỗi, mutation bỏ `WHERE`
- [x] Chia đợt backend+migration → frontend → chốt
- [x] Không thêm thư viện; phù hợp CLAUDE.md (phân lớp, ProblemDetails, migration, hệ thiết kế)

## Notes

- Chưa kiểm RPD thực của key Gemini để đặt trần global — ghi ở "Rủi ro đã chấp nhận", kiểm sau.
