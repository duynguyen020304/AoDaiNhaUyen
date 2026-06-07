import { Send, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface ChatInputProps {
  value: string
  onChange: (v: string) => void
  onSend: () => void
  isLoading: boolean
  placeholder?: string
}

export function ChatInput({ value, onChange, onSend, isLoading, placeholder }: ChatInputProps) {
  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      onSend()
    }
  }

  return (
    <div className="flex gap-2">
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder || 'Nhập yêu cầu...'}
        disabled={isLoading}
        className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-wine/30 disabled:opacity-50"
      />
      <Button
        size="sm"
        onClick={onSend}
        disabled={isLoading || !value.trim()}
        className="bg-wine hover:bg-wine/90 text-white shrink-0"
      >
        {isLoading ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
      </Button>
    </div>
  )
}
