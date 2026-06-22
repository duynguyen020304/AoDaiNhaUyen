import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { Package, Users, Shield, FolderTree, LogOut, Menu, Image, LayoutDashboard, Bot, ClipboardList, Settings2, Tag, Newspaper, Megaphone, Send, PanelLeftClose, PanelLeftOpen, MessageSquareText, Sparkles, Share2, ChevronDown, ChevronRight } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { Button } from '@/components/ui/button'
import { Sheet, SheetHeader, SheetTrigger } from '@/components/ui/sheet'
import { AiChatSidebar } from '@/components/ai/AiChatSidebar'

const NAV_GROUPS = [
  {
    label: 'Tổng quan',
    items: [
      { to: '/admin/dashboard', icon: LayoutDashboard, label: 'Tổng quan', end: true },
    ],
  },
  {
    label: 'Bán hàng',
    items: [
      { to: '/admin/orders', icon: ClipboardList, label: 'Đơn hàng', end: false },
      { to: '/admin/promos', icon: Tag, label: 'Mã giảm giá', end: false },
    ],
  },
  {
    label: 'Sản phẩm',
    items: [
      { to: '/admin/products', icon: Package, label: 'Sản phẩm', end: false },
      { to: '/admin/categories', icon: FolderTree, label: 'Danh mục', end: false },
      { to: '/admin/media', icon: Image, label: 'Hình ảnh', end: false },
    ],
  },
  {
    label: 'Marketing & Nội dung',
    items: [
      { to: '/admin/marketing', icon: Megaphone, label: 'Marketing', end: true },
      { to: '/admin/blog', icon: Newspaper, label: 'Bài đăng', end: false },
      { to: '/admin/facebook', icon: Share2, label: 'Fanpage', end: true },
    ],
  },
  {
    label: 'Khách hàng',
    items: [
      { to: '/admin/users', icon: Users, label: 'Người dùng', end: false },
      { to: '/admin/reviews', icon: MessageSquareText, label: 'Đánh giá', end: false },
    ],
  },
  {
    label: 'AI & Tự động hóa',
    items: [
      { to: '/admin/hermes', icon: Bot, label: 'AI Chat', end: false },
      { to: '/admin/ai-tryon-feedback', icon: Sparkles, label: 'Đánh giá AI try-on', end: false },
      { to: '/admin/tools-risk', icon: Settings2, label: 'Cấu hình AI', end: false },
    ],
  },
  {
    label: 'Hệ thống',
    items: [
      { to: '/admin/roles', icon: Shield, label: 'Vai trò', end: false },
      { to: '/admin/email-queue', icon: Send, label: 'Hàng đợi email', end: false },
    ],
  },
] as const

function SidebarContent({ onNavigate, collapsed = false, onToggle }: { onNavigate?: () => void; collapsed?: boolean; onToggle?: () => void }) {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>(() =>
    NAV_GROUPS.reduce<Record<string, boolean>>((groups, group) => {
      groups[group.label] = true
      return groups
    }, {})
  )

  function toggleGroup(label: string) {
    setExpandedGroups((groups) => ({
      ...groups,
      [label]: !groups[label],
    }))
  }

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <>
      <div className={`flex items-center border-b border-white/10 ${collapsed ? 'justify-center p-4' : 'justify-between p-6'}`}>
        <div className={`flex items-center ${collapsed ? 'justify-center' : 'gap-3'}`}>
          <img
            src="/logo.svg"
            alt="Nhã Uyên"
            className={collapsed ? 'size-10 rounded-full p-1' : 'size-11 rounded-xl p-1.5'}
          />
          {!collapsed && (
            <div>
              <span className="text-gold font-bold text-lg leading-none">Nhã Uyên</span>
              <div className="text-white/60 text-xs mt-0.5">Admin</div>
            </div>
          )}
        </div>
        {onToggle && (
          <Button
            variant="ghost"
            size="icon"
            className="text-white/70 hover:bg-white/10 hover:text-white"
            onClick={onToggle}
            aria-label={collapsed ? 'Mở sidebar' : 'Đóng sidebar'}
            title={collapsed ? 'Mở sidebar' : 'Đóng sidebar'}
          >
            {collapsed ? <PanelLeftOpen className="size-5" /> : <PanelLeftClose className="size-5" />}
          </Button>
        )}
      </div>
      <nav className={`flex-1 overflow-y-auto ${collapsed ? 'space-y-3 p-3' : 'space-y-5 p-4'}`}>
        {NAV_GROUPS.map((group) => {
          const expanded = expandedGroups[group.label] !== false

          return (
            <div key={group.label} className={collapsed ? 'space-y-1.5' : 'space-y-2'}>
              {!collapsed && (
                <button
                  type="button"
                  onClick={() => toggleGroup(group.label)}
                  aria-expanded={expanded}
                  className="flex w-full items-center justify-between rounded-md px-3 py-1 text-left text-[11px] font-semibold uppercase tracking-[0.16em] text-white/45 transition-colors hover:bg-white/5 hover:text-white/75"
                >
                  <span>{group.label}</span>
                  {expanded ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
                </button>
              )}
              {(collapsed || expanded) && (
                <div className="space-y-1">
                  {group.items.map(({ to, icon: Icon, label, end }) => (
                    <NavLink
                      key={to}
                      to={to}
                      end={end}
                      onClick={onNavigate}
                      title={collapsed ? `${group.label} · ${label}` : undefined}
                      className={({ isActive }) =>
                        `flex items-center rounded-lg text-sm transition-colors ${collapsed ? 'justify-center px-2 py-2.5' : 'gap-3 px-3 py-2'} ${isActive ? 'bg-wine/40 text-white' : 'text-white/70 hover:bg-white/10 hover:text-white'}`
                      }
                    >
                      <Icon className="size-5 shrink-0" />
                      {!collapsed && label}
                    </NavLink>
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </nav>
      <div className={`border-t border-white/10 ${collapsed ? 'p-3' : 'p-4'}`}>
        {user && !collapsed && (
          <div className="text-white/80 text-xs mb-3 truncate">
            {user.fullName}
          </div>
        )}
        <Button
          variant="ghost"
          size={collapsed ? 'icon' : 'sm'}
          className={`${collapsed ? 'w-full' : 'w-full justify-start'} text-white/70 hover:bg-white/10 hover:text-white`}
          onClick={handleLogout}
          title={collapsed ? 'Đăng xuất' : undefined}
        >
          <LogOut className={collapsed ? 'size-4' : 'size-4 mr-2'} />
          {!collapsed && 'Đăng xuất'}
        </Button>
      </div>
    </>
  )
}

export function AdminSidebar() {
  const [open, setOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(false)

  return (
    <>
      {/* Mobile trigger */}
      <div className="lg:hidden fixed top-0 left-0 z-40 p-4">
        <SheetTrigger onClick={() => setOpen(true)}>
          <Button variant="ghost" size="icon" className="text-white bg-burgundy">
            <Menu className="size-5" />
          </Button>
        </SheetTrigger>
      </div>

      {/* Mobile sheet */}
      <Sheet open={open} onOpenChange={setOpen}>
        <div className="flex flex-col h-full bg-burgundy text-white">
          <SheetHeader onOpenChange={setOpen} className="border-white/10 bg-burgundy text-white">
            <span className="font-semibold">Menu</span>
          </SheetHeader>
          <SidebarContent onNavigate={() => setOpen(false)} />
        </div>
      </Sheet>

      {/* Desktop sidebar */}
      <aside className={`hidden lg:flex sticky top-0 h-dvh bg-burgundy text-white flex-col shrink-0 transition-[width] duration-200 ${collapsed ? 'w-20' : 'w-64'}`}>
        <SidebarContent collapsed={collapsed} onToggle={() => setCollapsed((value) => !value)} />
      </aside>
    </>
  )
}

export function AdminLayout() {
  const isAiOpen = useAdminAiStore((s) => s.isOpen)
  const toggleAi = useAdminAiStore((s) => s.toggle)
  const closeAi = useAdminAiStore((s) => s.close)
  const location = useLocation()
  const isChatPage = location.pathname.startsWith('/admin/hermes')

  // Auto-close widget when navigating to full chat page
  useEffect(() => {
    if (isChatPage && isAiOpen) {
      closeAi()
    }
  }, [isChatPage, isAiOpen, closeAi])

  return (
    <div className="flex h-dvh overflow-hidden">
      <AdminSidebar />
      <main className="flex-1 bg-cream p-4 lg:p-6 overflow-y-auto pt-14 lg:pt-6">
        <Outlet />
      </main>

      {/* FAB + widget hidden on full chat page */}
      {!isChatPage && (
        <>
          {/* Quick Chat FAB */}
          <button
            onClick={toggleAi}
            className={`fixed bottom-4 right-4 z-40 p-3 rounded-full shadow-lg transition-all ${
              isAiOpen
                ? 'bg-gray-200 text-gray-600'
                : 'bg-wine text-white hover:bg-wine/90'
            }`}
            aria-label="AI Trợ lý"
            title="AI Trợ lý"
          >
            <Bot className="size-5" />
          </button>

          {/* Quick Chat sidebar widget */}
          <AiChatSidebar />
        </>
      )}
    </div>
  )
}
