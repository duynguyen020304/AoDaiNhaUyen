import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Sparkles, TrendingUp, Package, Users, FileText } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import type { AiSuggestion } from '@/types/ai'

const iconMap: Record<string, typeof Sparkles> = {
  '📊': TrendingUp,
  '📦': Package,
  '👥': Users,
  '📝': FileText,
}

export function AiSuggestionCards() {
  const suggestions = useAdminAiStore((s) => s.suggestions)
  const fetchSuggestions = useAdminAiStore((s) => s.fetchSuggestions)
  const open = useAdminAiStore((s) => s.open)
  const sendMessage = useAdminAiStore((s) => s.sendMessage)
  const navigate = useNavigate()

  useEffect(() => {
    fetchSuggestions()
  }, [fetchSuggestions])

  if (suggestions.length === 0) return null

  function handleClick(s: AiSuggestion) {
    if (s.route) {
      navigate(s.route)
    } else {
      open()
      sendMessage({ message: s.title })
    }
  }

  return (
    <div className="mb-6">
      <div className="flex items-center gap-2 mb-3">
        <Sparkles className="size-4 text-wine" />
        <h3 className="text-sm font-semibold text-gray-600">Gợi ý từ AI</h3>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {suggestions.map((s) => {
          const prefix = s.title.slice(0, 2)
          const Icon = iconMap[prefix] || Sparkles
          return (
            <button
              key={s.id}
              onClick={() => handleClick(s)}
              className="flex items-start gap-3 p-3 bg-white border border-gray-200 rounded-xl hover:border-wine/30 hover:shadow-sm transition-all text-left"
            >
              <div className="p-2 bg-wine/10 rounded-lg shrink-0">
                <Icon className="size-4 text-wine" />
              </div>
              <div className="min-w-0">
                <div className="text-sm font-medium text-gray-800 truncate">
                  {s.title}
                </div>
                <div className="text-xs text-gray-500 mt-0.5 line-clamp-2">
                  {s.description}
                </div>
              </div>
            </button>
          )
        })}
      </div>
    </div>
  )
}
