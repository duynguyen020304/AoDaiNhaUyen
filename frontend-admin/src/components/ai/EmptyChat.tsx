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
    <div className="flex-1 flex flex-col items-center justify-center px-6 py-12">
      <div className="size-16 bg-wine/5 border border-wine/15 rounded-2xl flex items-center justify-center mb-6 shadow-sm">
        <Bot className="size-8 text-wine animate-pulse" />
      </div>
      <p className="text-sm text-gray-500 text-center max-w-sm mb-8 leading-relaxed">
        Hỏi tôi bất cứ điều gì về cửa hàng — doanh thu, sản phẩm, đơn hàng, khách hàng. Tôi có thể đọc dữ liệu và thực hiện thao tác giúp bạn.
      </p>
      
      <div className="w-full max-w-lg">
        <div className="flex items-center gap-1.5 px-1 mb-3 text-[10px] font-bold text-gray-400 uppercase tracking-widest">
          <Sparkles className="size-3 text-gold" />
          <span>Gợi ý nhanh</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full">
          {QUICK_SUGGESTIONS.map((s, i) => (
            <button
              key={i}
              onClick={() => onSuggestionClick(s.message)}
              disabled={isLoading}
              className="flex items-center gap-3 p-3.5 bg-white border border-gray-200/80 rounded-xl hover:border-wine/30 hover:shadow-md hover:shadow-wine/[0.02] transition-all duration-200 text-left disabled:opacity-50 active:scale-98 cursor-pointer"
            >
              <div className="p-2 bg-wine/5 rounded-lg shrink-0">
                <s.icon className="size-4 text-wine" />
              </div>
              <span className="text-sm font-medium text-gray-700">{s.label}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
