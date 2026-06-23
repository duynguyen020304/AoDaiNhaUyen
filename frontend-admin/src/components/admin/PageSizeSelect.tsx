import { Select } from '@/components/ui/select'

const DEFAULT_OPTIONS = [10, 20, 50, 100]

interface PageSizeSelectProps {
  value: number
  onChange: (value: number) => void
  options?: number[]
  disabled?: boolean
  label?: string
}

export function PageSizeSelect({
  value,
  onChange,
  options = DEFAULT_OPTIONS,
  disabled = false,
  label = 'Hiển thị',
}: PageSizeSelectProps) {
  return (
    <label className="flex items-center gap-2 text-sm text-muted-foreground">
      <span>{label}</span>
      <Select
        className="h-8 w-24 pr-8"
        value={String(value)}
        disabled={disabled}
        onChange={(event) => onChange(Number(event.target.value))}
      >
        {options.map((option) => (
          <option key={option} value={option}>{option}/trang</option>
        ))}
      </Select>
    </label>
  )
}
