import { ApiError, describeApiError } from './apiError'

/** MỘT bảng duy nhất: mã lỗi máy đọc được của backend (trường `code` trong ProblemDetails) → thông điệp tiếng Việt. */
export const AVATAR_ERROR_MESSAGES: Record<string, string> = {
  item_not_found: 'Không tìm thấy món đồ này.',
  item_not_purchasable: 'Món này miễn phí, không cần mua.',
  already_owned: 'Bạn đã sở hữu món này rồi.',
  insufficient_coins: 'Bạn không đủ Xu để mua món này.',
  unknown_variant: 'Có kiểu không còn trong danh mục. Hãy tải lại trang rồi chọn lại.',
  item_not_owned: 'Bạn chưa sở hữu một món đã chọn. Hãy mua món đó ở cửa hàng hoặc chọn món khác.',
  graphic_incompatible_clothes: 'Hình in chỉ dùng được với áo 01 đến 10. Hãy đổi áo hoặc bỏ hình in.',
  validation_failed: 'Dữ liệu chưa hợp lệ. Hãy kiểm tra lại các ô bắt buộc.',
}

/** Thông điệp cho lỗi của cửa hàng và nhân vật: ưu tiên mã lỗi riêng, còn lại dùng câu lỗi chung của dự án. */
export function describeAvatarError(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code) {
    const message = AVATAR_ERROR_MESSAGES[error.problem.code]
    if (message) return message
  }

  return describeApiError(error)
}
