export interface GenerateError {
  message: string
  /** Số câu hỏi của phòng tại thời điểm lỗi - để biết lỗi đã cũ khi danh sách câu đổi (thêm/xóa thủ công). */
  atCount: number | undefined
}

/** Lỗi sinh câu chỉ còn đúng khi số câu hỏi hiện tại vẫn như lúc lỗi xảy ra. */
export function shouldShowGenerateError(error: GenerateError | undefined, currentCount: number | undefined): boolean {
  return error !== undefined && error.atCount === currentCount
}
