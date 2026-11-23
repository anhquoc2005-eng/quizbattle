interface PracticeOptionsProps {
  options: string[]
  selected: number | null
  disabled: boolean
  onSelect: (index: number) => void
  /** Có sau khi nộp: tô đáp án đúng và (nếu chọn sai) đáp án đã chọn. */
  correctIndex?: number
  chosenIndex?: number
}

export function PracticeOptions({ options, selected, disabled, onSelect, correctIndex, chosenIndex }: PracticeOptionsProps) {
  const graded = correctIndex !== undefined

  return (
    <div className="grid gap-2.5" role="group" aria-label="Các đáp án">
      {options.map((option, index) => {
        const isCorrect = graded && index === correctIndex
        const isWrongChoice = graded && index === chosenIndex && index !== correctIndex
        const border = isCorrect ? 'border-positive' : isWrongChoice ? 'border-negative' : index === selected ? 'border-ink' : 'border-control'

        return (
          <button
            key={index}
            type="button"
            disabled={disabled}
            aria-pressed={graded ? index === chosenIndex : index === selected}
            onClick={() => onSelect(index)}
            className={`flex min-h-11 w-full items-center justify-between gap-3 rounded-[14px] border px-4 py-3 text-left ${border}`}
          >
            <span>{option}</span>
            {isCorrect && <span className="text-xs text-positive">Đáp án đúng</span>}
            {isWrongChoice && <span className="text-xs text-negative">Bạn chọn</span>}
          </button>
        )
      })}
    </div>
  )
}
