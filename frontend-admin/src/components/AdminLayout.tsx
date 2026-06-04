import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Package, Users, Shield, FolderTree, LogOut, Menu } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'
import { Button } from '@/components/ui/button'
import { Sheet, SheetHeader, SheetTrigger } from '@/components/ui/sheet'

const NAV_ITEMS = [
  { to: '/admin/products', icon: Package, label: 'Sản phẩm', end: true },
  { to: '/admin/categories', icon: FolderTree, label: 'Danh mục', end: false },
  { to: '/admin/users', icon: Users, label: 'Người dùng', end: false },
  { to: '/admin/roles', icon: Shield, label: 'Vai trò', end: false },
] as const

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <>
      <div className="p-6 border-b border-white/10">
        <span className="text-gold font-bold text-lg">Nhã Uyên</span>
        <div className="text-white/60 text-xs mt-0.5">Admin</div>
      </div>
      <nav className="flex-1 p-4 space-y-1">
        {NAV_ITEMS.map(({ to, icon: Icon, label, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            onClick={onNavigate}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${isActive ? 'bg-wine/40 text-white' : 'text-white/70 hover:bg-white/10 hover:text-white'}`
            }
          >
            <Icon className="size-5" />
            {label}
          </NavLink>
        ))}
      </nav>
      <div className="p-4 border-t border-white/10">
        {user && (
          <div className="text-white/80 text-xs mb-3 truncate">
            {user.fullName}
          </div>
        )}
        <Button
          variant="ghost"
          size="sm"
          className="w-full justify-start text-white/70 hover:bg-white/10 hover:text-white"
          onClick={handleLogout}
        >
          <LogOut className="size-4 mr-2" />
          Đăng xuất
        </Button>
      </div>
    </>
  )
}

export function AdminSidebar() {
  const [open, setOpen] = useState(false)

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
      <aside className="hidden lg:flex sticky top-0 w-64 min-h-dvh bg-burgundy text-white flex-col shrink-0">
        <SidebarContent />
      </aside>
    </>
  )
}

export function AdminLayout() {
  return (
    <div className="flex min-h-dvh">
      <AdminSidebar />
      <main className="flex-1 bg-cream p-4 lg:p-6 overflow-y-auto pt-14 lg:pt-6">
        <Outlet />
      </main>
    </div>
  )
}
