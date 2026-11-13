/** Số dư Xu của người đang đăng nhập (server là nguồn sự thật). */
export function CoinBalance({ coins }: { coins: number }) {
  return (
    <p className="text-muted">
      Số dư:{' '}
      <span data-balance className="font-semibold tabular-nums text-ink">
        {coins} Xu
      </span>
    </p>
  )
}
