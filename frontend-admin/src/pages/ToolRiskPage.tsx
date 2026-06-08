import { useState, useEffect } from 'react'
import { Loader2, Settings2, Bot, AlertTriangle, CheckCircle2, Activity } from 'lucide-react'
import { request } from '@/lib/client'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Button } from '@/components/ui/button'

interface ToolRiskConfig {
  id: string
  toolName: string
  riskLevel: string
  requiresConfirmation: boolean
  description: string | null
  category: string | null
}

const RISK_LEVELS = ['Read', 'Low', 'Medium', 'High', 'Critical'] as const

const RISK_BADGE: Record<string, string> = {
  Read: 'bg-zinc-100 text-zinc-800 dark:bg-zinc-800 dark:text-zinc-200',
  Low: 'bg-emerald-50 text-emerald-700 border border-emerald-200 dark:bg-emerald-950/30 dark:text-emerald-300',
  Medium: 'bg-amber-50 text-amber-800 border border-amber-200 dark:bg-amber-950/30 dark:text-amber-300 dark:border-amber-900/50',
  High: 'bg-red-50 text-red-900 border border-red-200 dark:bg-red-950/20 dark:text-red-300 dark:border-red-900/30',
  Critical: 'bg-red-100 text-red-900 border border-red-300 dark:bg-red-950/40 dark:text-red-200',
}

export function ToolRiskPage() {
  const [configs, setConfigs] = useState<ToolRiskConfig[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState<string | null>(null)
  const [autoMode, setAutoMode] = useState(false)

  useEffect(() => {
    loadConfigs()
    loadAutoMode()
  }, [])

  async function loadConfigs() {
    setLoading(true)
    try {
      const res = await request<{ data: ToolRiskConfig[] }>('/api/admin/tools-risk')
      setConfigs(res.data)
    } catch {
      // silent
    } finally {
      setLoading(false)
    }
  }

  async function loadAutoMode() {
    try {
      const res = await request<{ data: { isAutoMode: boolean } }>('/api/admin/ai/auto-mode/status')
      setAutoMode(res.data.isAutoMode)
    } catch {
      // silent
    }
  }

  async function toggleAutoMode() {
    try {
      await request('/api/admin/ai/auto-mode/toggle', {
        method: 'POST',
        body: JSON.stringify({ enabled: !autoMode }),
      })
      setAutoMode(!autoMode)
    } catch {
      // silent
    }
  }

  async function updateConfig(config: ToolRiskConfig, riskLevel: string, requiresConfirmation: boolean) {
    setSaving(config.id)
    try {
      await request(`/api/admin/tools-risk/${config.id}`, {
        method: 'PUT',
        body: JSON.stringify({ riskLevel, requiresConfirmation }),
      })
      setConfigs(prev =>
        prev.map(c =>
          c.id === config.id
            ? { ...c, riskLevel, requiresConfirmation }
            : c
        )
      )
    } catch {
      // silent
    } finally {
      setSaving(null)
    }
  }

  // Group by category
  const grouped = configs.reduce<Record<string, ToolRiskConfig[]>>((acc, c) => {
    const cat = c.category || 'Other'
    if (!acc[cat]) acc[cat] = []
    acc[cat].push(c)
    return acc
  }, {})

  const stats = {
    total: configs.length,
    autoApproved: configs.filter(c => c.riskLevel === 'Read' || c.riskLevel === 'Low').length,
    needsConfirmation: configs.filter(c => c.requiresConfirmation).length,
  }

  return (
    <div className="flex flex-col lg:flex-row gap-6">
      {/* Main: Tool table */}
      <div className="flex-1 space-y-6 min-w-0">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">
            Cấu hình rủi ro công cụ AI
          </h1>
          <p className="text-sm text-zinc-500 mt-1">
            Quản lý mức độ rủi ro và quyền tự động cho từng công cụ AI
          </p>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="size-6 animate-spin text-primary" />
          </div>
        ) : (
          Object.entries(grouped).map(([category, tools]) => (
            <div key={category} className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden">
              <div className="px-4 py-3 bg-zinc-50 dark:bg-zinc-800/50 border-b border-zinc-200 dark:border-zinc-800 flex items-center gap-2">
                <Settings2 className="size-4 text-zinc-400" />
                <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">{category}</span>
                <span className="text-xs text-zinc-400 ml-auto">{tools.length} công cụ</span>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Công cụ</TableHead>
                    <TableHead>Mô tả</TableHead>
                    <TableHead>Mức rủi ro</TableHead>
                    <TableHead className="text-center">Tự động</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tools.map((tool) => (
                    <TableRow key={tool.id} className="hover:bg-zinc-50 dark:hover:bg-zinc-900/50 transition-colors">
                      <TableCell className="font-mono text-xs">{tool.toolName}</TableCell>
                      <TableCell className="text-sm text-zinc-600 dark:text-zinc-400">
                        {tool.description || '—'}
                      </TableCell>
                      <TableCell>
                        <select
                          value={tool.riskLevel}
                          onChange={(e) => updateConfig(tool, e.target.value, tool.requiresConfirmation)}
                          disabled={saving === tool.id}
                          className="rounded-md border border-zinc-200 dark:border-zinc-700 px-2 py-1 text-sm bg-white dark:bg-zinc-800 transition-colors disabled:opacity-50"
                        >
                          {RISK_LEVELS.map(level => (
                            <option key={level} value={level}>{level}</option>
                          ))}
                        </select>
                        <span className={`ml-2 inline-block px-2 py-0.5 rounded-full text-xs font-medium ${RISK_BADGE[tool.riskLevel] || ''}`}>
                          {tool.riskLevel}
                        </span>
                      </TableCell>
                      <TableCell className="text-center">
                        <button
                          onClick={() => updateConfig(tool, tool.riskLevel, !tool.requiresConfirmation)}
                          disabled={saving === tool.id}
                          className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors disabled:opacity-50 ${
                            tool.requiresConfirmation ? 'bg-primary' : 'bg-zinc-300 dark:bg-zinc-600'
                          }`}
                        >
                          <span className={`inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform ${
                            tool.requiresConfirmation ? 'translate-x-4' : 'translate-x-0.5'
                          }`} />
                        </button>
                      </TableCell>
                      <TableCell className="text-right">
                        {saving === tool.id && (
                          <Loader2 className="size-4 animate-spin text-primary inline-block" />
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ))
        )}
      </div>

      {/* Sidebar: Stats + Auto Mode */}
      <aside className="w-full lg:w-72 shrink-0 space-y-4">
        {/* Stats */}
        <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-4 space-y-3">
          <h3 className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Thống kê</h3>
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-sm text-zinc-500">Tổng công cụ</span>
              <span className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{stats.total}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-zinc-500">Tự động duyệt</span>
              <span className="text-sm font-semibold text-emerald-600">{stats.autoApproved}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-zinc-500">Cần xác nhận</span>
              <span className="text-sm font-semibold text-amber-600">{stats.needsConfirmation}</span>
            </div>
          </div>
        </div>

        {/* Auto Mode Toggle */}
        <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-4 space-y-3">
          <div className="flex items-center gap-2">
            <Bot className="size-4 text-primary" />
            <h3 className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Chế độ tự động</h3>
          </div>
          <p className="text-xs text-zinc-500">
            Khi bật, các hành động Medium risk sẽ được tự động thực hiện mà không cần xác nhận.
          </p>
          <Button
            onClick={toggleAutoMode}
            className={`w-full active:scale-[0.98] transition-transform ${
              autoMode
                ? 'bg-[#721311] hover:bg-[#870e0b] text-white'
                : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:hover:bg-zinc-700 dark:text-zinc-300'
            }`}
          >
            {autoMode ? (
              <>
                <CheckCircle2 className="size-4 mr-2" />
                Đang bật — Tắt đi
              </>
            ) : (
              <>
                <AlertTriangle className="size-4 mr-2" />
                Đang tắt — Bật lên
              </>
            )}
          </Button>
        </div>

        {/* Activity hint */}
        <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-4 space-y-3">
          <div className="flex items-center gap-2">
            <Activity className="size-4 text-zinc-400" />
            <h3 className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Hoạt động gần đây</h3>
          </div>
          <p className="text-xs text-zinc-500">
            Mọi hành động của AI đều được ghi lại trong nhật ký. Xem chi tiết tại trang AI Trợ lý.
          </p>
        </div>
      </aside>
    </div>
  )
}
