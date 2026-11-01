/** Khối lỗi + nút "Thử lại", dùng chung cho mọi khối gọi dữ liệu (dashboard, trang phòng). */
export function ErrorRetry({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="mt-4 grid justify-items-start gap-3 rounded-[14px] border border-dashed border-control p-5">
      <p className="text-negative">{message}</p>
      <button className="btn btn-ghost" type="button" onClick={onRetry}>
        Thử lại
      </button>
    </div>
  )
}
