import { Bot, Sparkles, TrendingUp, Package, Users } from 'lucide-react'

const QUICK_SUGGESTIONS = [
  { icon: TrendingUp, label: 'Xem báo cáo doanh thu tuần này', message: 'Xem báo cáo doanh thu tuần này' },
  { icon: Package, label: 'Sản phẩm nào bán chạy nhất?', message: 'Sản phẩm nào bán chạy nhất?' },
  { icon: Users, label: 'Top khách hàng tháng này', message: 'Top khách hàng tháng này' },
  { icon: Sparkles, label: 'Tổng quan cửa hàng hôm nay', message: 'Tổng quan cửa hàng hôm nay' },
]

interface EmptyChatProps {
  onSuggestionClick: (message: string) => void
  isLoading: boolean
}

export function EmptyChat({ onSuggestionClick, isLoading }: EmptyChatProps) {
  return (
    <div className="flex-1 flex flex-col items-center justify-center text-gray-400 px-6">
      <div className="p-4 bg-wine/10 rounded-full mb-6">
        <Bot className="size-12 text-wine" />
      </div>
      <h2 className="text-xl font-semibold text-gray-700 mb-2">
        Trợ lý AI Admin
      </h2>
      <p className="text-sm text-center max-w-md mb-8">
        Hỏi tôi bất cứ điều gì về cửa hàng — doanh thu, sản phẩm, đơn hàng, khách hàng.
        Tôi có thể đọc dữ liệu và thực hiện thao tác giúp bạn.
      </p>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full max-w-lg">
        {QUICK_SUGGESTIONS.map((s, i) => (
          <button
            key={i}
            onClick={() => onSuggestionClick(s.message)}
            disabled={isLoading}
            className="flex items-center gap-3 p-3 bg-white border border-gray-200 rounded-xl hover:border-wine/30 hover:shadow-sm transition-all text-left disabled:opacity-50"
          >
            <div className="p-2 bg-wine/10 rounded-lg shrink-0">
              <s.icon className="size-4 text-wine" />
            </div>
            <span className="text-sm text-gray-700">{s.label}</span>
          </button>
        ))}
      </div>
    </div>
  )
}
