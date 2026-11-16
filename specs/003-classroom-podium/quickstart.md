# Quickstart: Kiểm chứng lớp học + bục vinh danh

## Chuẩn bị

```powershell
docker compose up -d                      # PostgreSQL dev
cd backend; dotnet build; dotnet test     # phải xanh
cd ../frontend; npm run build; npm test   # phải xanh
```

Chạy dev: backend `dotnet run --project QuizBattle.Api`, frontend `npm run dev`.

## Kịch bản 1 — Giơ tay + ✓/✗ (User Story 1)

1. Đăng nhập, tạo phòng, duyệt vài câu hỏi, mở màn chủ phòng (RoomLivePage).
2. Mở 3 tab ẩn danh vào `/join?code=...` làm người chơi A, B, C. → Sảnh chủ phòng hiện 3 bàn học
   (nhân vật + tên).
3. Bắt đầu trận. Người A trả lời → **trong ~1 giây** nhân vật A giơ tay trên màn chủ phòng
   (SC-001); màn B/C không có dấu hiệu gì về việc A đã trả lời.
4. B trả lời sai, C không trả lời. Chờ hết giờ → A có ✓, B và C có ✗, nhân vật nhún nhẹ, hình
   DiceBear không đổi (SC-003).
5. Sang câu mới → mọi tay/huy hiệu sạch (FR-009).
6. DevTools tab Network (WS) ở tab người chơi: không frame nào trước QuestionEnded chứa
   `answeredPlayerIds` (SC-002 — đã có test tự động, đây là soát tay thêm).

## Kịch bản 2 — Chủ phòng kết nối lại giữa câu (FR-008, SC-007)

1. Trong một câu, cho A trả lời rồi đóng hẳn tab chủ phòng.
2. Mở lại màn chủ phòng khi câu còn chạy → nhân vật A vẫn giơ tay (từ RoomState, không cần chờ
   người tiếp theo trả lời).

## Kịch bản 3 — Bục vinh danh (User Story 2)

1. Chơi hết trận với 3 người điểm khác nhau → cả màn chủ phòng lẫn màn người chơi: bục lộ dần
   hạng 3 → 2 → 1, pháo giấy; RewardSummary của người chơi vẫn hiện như cũ (FR-019).
2. Trận 2 người → chỉ 2 bậc, không bậc trống (FR-016).
3. Đồng hạng (hai người bằng điểm cao nhất) → chung bậc 1, không bậc 2 (FR-015).
4. Người chơi đóng tab, vào lại phòng đã Finished → vẫn thấy bục (FR-018).

## Kịch bản 4 — Reduced motion + 50 người

1. DevTools → Rendering → Emulate `prefers-reduced-motion: reduce` → màn Finished hiện ngay,
   không lộ dần, không pháo giấy; trong câu không nhún (SC-006).
2. Giả lập 50 người (script join nhanh hoặc hạ tạm MAX bằng dữ liệu thật nếu có): màn chủ phòng
   1366px hiện đủ 50 bàn, không cuộn ngang, chuyển câu không khựng thấy rõ (SC-004).

## Test tự động phải có (chạy trong CI từng đợt)

- xUnit: `GameServiceTests` — AnsweredPlayerIds không rò sang người chơi; CorrectPlayerIds với
  người không trả lời/sai/rời phòng; RoomState host reconnect.
- Vitest: `podiumSteps.test.ts` (đồng hạng, 1–2 người, rỗng); `gameReducer.test.ts` (AnswerProgress
  trễ bị bỏ, reset khi sang câu, nạp từ RoomState).
- jsdom: `Classroom.test.tsx`, `Podium.test.tsx`.
