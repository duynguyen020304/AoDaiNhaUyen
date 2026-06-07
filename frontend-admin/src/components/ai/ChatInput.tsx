import { Send, Loader2 } from 'lucide-react'

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
    <div className="flex items-center gap-2 bg-white border border-gray-200 shadow-[0_8px_30px_rgb(0,0,0,0.04)] rounded-2xl p-2 transition-all focus-within:border-wine/30 focus-within:shadow-[0_8px_30px_rgb(79,13,12,0.06)]">
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder || 'Nhập yêu cầu...'}
        disabled={isLoading}
        className="flex-1 px-4 py-2.5 bg-transparent border-0 text-sm focus:outline-none focus:ring-0 disabled:opacity-50 text-gray-800 placeholder:text-gray-400"
      />
      <button
        onClick={onSend}
        disabled={isLoading || !value.trim()}
        className="flex items-center justify-center size-9.5 bg-wine hover:bg-wine/90 disabled:bg-gray-100 disabled:text-gray-400 text-white rounded-xl shrink-0 transition-all duration-200 active:scale-95 cursor-pointer shadow-sm disabled:cursor-not-allowed"
      >
        {isLoading ? <Loader2 className="size-4.5 animate-spin" /> : <Send className="size-4" />}
      </button>
    </div>
  )
}
