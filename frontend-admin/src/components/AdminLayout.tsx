import { Outlet, NavLink } from 'react-router-dom'
import { useState } from 'react'
import { Package, Menu } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Sheet, SheetHeader, SheetTrigger } from '@/components/ui/sheet'

function SidebarContent() {
  return (
    <>
      <div className="p-6 border-b border-white/10">
        <span className="text-gold font-bold text-lg">Nhã Uyên</span>
        <div className="text-white/60 text-xs mt-0.5">Admin</div>
      </div>
      <nav className="flex-1 p-4 space-y-1">
        <NavLink
          to="/admin/products"
          end
          className={({ isActive }) =>
            `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${isActive ? 'bg-wine/40 text-white' : 'text-white/70 hover:bg-white/10 hover:text-white'}`
          }
        >
          <Package className="size-5" />
          Sản phẩm
        </NavLink>
      </nav>
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
          <SidebarContent />
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
