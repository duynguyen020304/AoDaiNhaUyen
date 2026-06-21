import { z } from 'zod'

// ─────────────────────────────────────────────────────────────────────────────
// Brand palette (mirrors the 4 dashboard charts + OrdersByStatusChart colors).
// ─────────────────────────────────────────────────────────────────────────────
export const BRAND_COLOR = '#721311'

export const CATEGORICAL_PALETTE = [
  '#721311',
  '#dc2626',
  '#f59e0b',
  '#16a34a',
  '#2563eb',
  '#7c3aed',
  '#6366f1',
  '#8b5cf6',
  '#6b7280',
]

// Map common status keys so order-status pie/donut matches the dashboard.
export const STATUS_COLORS: Record<string, string> = {
  pending: '#f59e0b',
  confirmed: '#3b82f6',
  processing: '#8b5cf6',
  shipping: '#6366f1',
  completed: '#16a34a',
  cancelled: '#dc2626',
  returned: '#6b7280',
}

export const AXIS_TICK_STYLE = { fontSize: 12, fill: '#6a7282' }
export const GRID_STROKE = '#f0f0f0'
export const TOOLTIP_STYLE = {
  borderRadius: '8px',
  border: '1px solid #f0f0f0',
  fontSize: '13px',
} as const

export type ChartFormat = 'currency' | 'number' | 'percent'

// ─────────────────────────────────────────────────────────────────────────────
// Zod schema — strict allowlist. Unknown keys are stripped (Zod default), never
// forwarded to Recharts. This is the security boundary: the AI payload is JSON
// in / props out, with no eval / Function / dangerouslySetInnerHTML anywhere.
// ─────────────────────────────────────────────────────────────────────────────
const FormatSchema = z.enum(['currency', 'number', 'percent'])

export const SeriesSchema = z.object({
  key: z.string(),
  name: z.string().optional(),
  color: z.string().optional(),
  type: z.enum(['line', 'bar', 'area']).optional(),
  stackId: z.string().optional(),
  yAxisId: z.string().optional(),
  dashed: z.boolean().optional(),
  marker: z.boolean().optional(),
})

export const YAxisDefSchema = z.object({
  id: z.string().default('left'),
  orientation: z.enum(['left', 'right']).optional(),
  formatValueAs: FormatSchema.optional(),
  label: z.string().optional(),
})

export const ReferenceLineSchema = z.object({
  yAxisId: z.string().default('left'),
  value: z.number(),
  label: z.string().optional(),
  color: z.string().optional(),
})

export const ChartSpecSchema = z.object({
  kind: z.enum([
    'line',
    'area',
    'bar',
    'horizontalBar',
    'stacked',
    'pie',
    'donut',
    'scatter',
    'radar',
    'radialBar',
    'composed',
  ]),
  title: z.string().optional(),
  subtitle: z.string().optional(),
  data: z.array(z.record(z.string(), z.any())),
  xAxisKey: z.string().optional(),
  xKey: z.string().optional(),
  yKey: z.string().optional(),
  series: z.array(SeriesSchema).optional(),
  yAxis: z.array(YAxisDefSchema).optional(),
  referenceLines: z.array(ReferenceLineSchema).optional(),
  valueKey: z.string().optional(),
  nameKey: z.string().optional(),
  colors: z.array(z.string()).optional(),
  formatValueAs: FormatSchema.optional(),
  height: z.number().min(160).max(560).optional(),
  legend: z.boolean().default(true),
})

export type ChartSpec = z.infer<typeof ChartSpecSchema>
export type ChartSeries = z.infer<typeof SeriesSchema>
export type ChartYAxisDef = z.infer<typeof YAxisDefSchema>
export type ChartReferenceLine = z.infer<typeof ReferenceLineSchema>
