/** Nhãn + ô nhập + lỗi/gợi ý, dùng chung cho form đăng nhập/đăng ký. Dùng class .field/.error có sẵn trong index.css. */
export function FormField({
  id,
  label,
  type = 'text',
  autoComplete,
  value,
  onChange,
  error,
  hint,
}: {
  id: string
  label: string
  type?: string
  autoComplete?: string
  value: string
  onChange: (value: string) => void
  error?: string
  hint?: string
}) {
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined

  return (
    <div className="field" data-invalid={error ? true : undefined}>
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        type={type}
        autoComplete={autoComplete}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        aria-describedby={describedBy}
      />
      {error ? (
        <p id={`${id}-error`} className="error">
          {error}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="text-xs text-faint">
          {hint}
        </p>
      ) : null}
    </div>
  )
}
