import { useMemo } from 'react'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ComposedChart,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  PolarAngleAxis,
  PolarGrid,
  PolarRadiusAxis,
  Radar,
  RadarChart,
  RadialBar,
  RadialBarChart,
  ReferenceLine,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import {
  AXIS_TICK_STYLE,
  BRAND_COLOR,
  CATEGORICAL_PALETTE,
  GRID_STROKE,
  STATUS_COLORS,
  TOOLTIP_STYLE,
  type ChartFormat,
  type ChartReferenceLine,
  type ChartSeries,
  type ChartSpec,
  type ChartYAxisDef,
} from './chartSpec'

// Re-export the schema so callers (MessageBubble) can validate without a second import.
export { ChartSpecSchema } from './chartSpec'
export type { ChartSpec } from './chartSpec'

// ─────────────────────────────────────────────────────────────────────────────
// Value formatting — matches RevenueChart.formatTooltip and SocialAnalyticsChart.
// ─────────────────────────────────────────────────────────────────────────────
function makeFormatter(format?: ChartFormat) {
  return (value: number | string) => {
    const n = Number(value) || 0
    if (format === 'currency') {
      if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}tr ₫`
      return `${n.toLocaleString('vi-VN')} ₫`
    }
    if (format === 'percent') return `${n}%`
    return n.toLocaleString('vi-VN')
  }
}

function resolveColor(
  explicit: string | undefined,
  palette: string[],
  index: number,
  fallbackKey?: string,
): string {
  if (explicit) return explicit
  // Auto-map common status keys so order-status pie/donut matches the dashboard.
  const key = fallbackKey?.toLowerCase()
  if (key && STATUS_COLORS[key]) return STATUS_COLORS[key]
  return palette[index % palette.length] || BRAND_COLOR
}

// ─────────────────────────────────────────────────────────────────────────────
// Sub-renderers. Each returns the inner chart element tree; the wrapper handles
// the card chrome, heading, and ResponsiveContainer sizing.
// ─────────────────────────────────────────────────────────────────────────────

interface AxisProps {
  yAxisDefs: ChartYAxisDef[]
  defaultFormat?: ChartFormat
  hasDualAxis: boolean
}

function renderYAxes({ yAxisDefs, defaultFormat, hasDualAxis }: AxisProps) {
  if (yAxisDefs.length === 0) {
    return (
      <YAxis
        width={68}
        tickFormatter={makeFormatter(defaultFormat)}
        tick={{ ...AXIS_TICK_STYLE, fontSize: 11 }}
        axisLine={false}
        tickLine={false}
      />
    )
  }
  return yAxisDefs.map((def, i) => {
    const orientation = def.orientation ?? (i === 1 ? 'right' : 'left')
    return (
      <YAxis
        key={def.id}
        yAxisId={hasDualAxis ? def.id : undefined}
        orientation={orientation}
        width={orientation === 'right' ? 46 : 68}
        tickFormatter={makeFormatter(def.formatValueAs ?? defaultFormat)}
        tick={{ ...AXIS_TICK_STYLE, fontSize: 11 }}
        axisLine={false}
        tickLine={false}
      />
    )
  })
}

function renderReferenceLines(lines: ChartReferenceLine[] | undefined, hasDualAxis: boolean) {
  if (!lines || lines.length === 0) return null
  return lines.map((rl, i) => (
    <ReferenceLine
      key={`ref-${i}`}
      y={rl.value}
      yAxisId={hasDualAxis ? rl.yAxisId : undefined}
      stroke={rl.color ?? '#16a34a'}
      strokeDasharray="6 4"
      label={{ value: rl.label, position: 'insideTopRight', style: { fill: rl.color ?? '#16a34a', fontSize: 11 } }}
    />
  ))
}

function renderSeries(spec: ChartSpec, palette: string[], forceType?: 'line' | 'bar' | 'area') {
  const series = spec.series ?? []
  return series.map((s: ChartSeries, i: number) => {
    const color = resolveColor(s.color, palette, i, s.key)
    const yAxisId = s.yAxisId
    const stackId = s.stackId ?? (spec.kind === 'stacked' ? 'stack' : undefined)
    const type = forceType ?? s.type ?? 'line'
    const strokeDasharray = s.dashed ? '6 4' : undefined
    const marker = s.marker

    if (type === 'bar') {
      return (
        <Bar
          key={s.key}
          dataKey={s.key}
          name={s.name ?? s.key}
          fill={color}
          stackId={stackId}
          yAxisId={yAxisId}
          radius={spec.kind === 'horizontalBar' ? [0, 8, 8, 0] : [8, 8, 0, 0]}
        />
      )
    }
    if (type === 'area') {
      return (
        <Area
          key={s.key}
          type="monotone"
          dataKey={s.key}
          name={s.name ?? s.key}
          stroke={color}
          strokeWidth={2}
          fill={`url(#chart-grad-${i})`}
          stackId={stackId}
          yAxisId={yAxisId}
        />
      )
    }
    // line (default). NOTE: Recharts v3 dropped stackId from Line — stacked
    // lines are not supported; use Area/Bar with stackId instead.
    return (
      <Line
        key={s.key}
        type="monotone"
        dataKey={s.key}
        name={s.name ?? s.key}
        stroke={color}
        strokeWidth={2}
        yAxisId={yAxisId}
        strokeDasharray={strokeDasharray}
        dot={marker ? { r: 3 } : false}
        activeDot={{ r: 5 }}
      />
    )
  })
}

function renderAreaDefs(series: ChartSpec['series'], palette: string[]) {
  if (!series) return null
  return (
    <defs>
      {series.map((s, i) => {
        const color = resolveColor(s.color, palette, i)
        return (
          <linearGradient key={`chart-grad-${i}`} id={`chart-grad-${i}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={color} stopOpacity={0.2} />
            <stop offset="95%" stopColor={color} stopOpacity={0} />
          </linearGradient>
        )
      })}
    </defs>
  )
}

interface CartesianLayout {
  margin: { top: number; right: number; left: number; bottom: number }
  xAxisHeight: number
  xAxisTick: typeof AXIS_TICK_STYLE & { angle?: number; textAnchor?: 'start' | 'middle' | 'end' }
}

interface CartesianProps {
  spec: ChartSpec
  palette: string[]
  hasDualAxis: boolean
  layout: CartesianLayout
}

function CartesianChart({ spec, palette, hasDualAxis, layout }: CartesianProps) {
  const format = spec.formatValueAs

  // Composed chart: render each series by its own type.
  if (spec.kind === 'composed') {
    return (
      <ComposedChart data={spec.data} margin={layout.margin}>
        {renderAreaDefs(spec.series, palette)}
        <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
        <XAxis dataKey={spec.xAxisKey} tick={layout.xAxisTick} height={layout.xAxisHeight} interval="preserveStartEnd" axisLine={false} tickLine={false} />
        {renderYAxes({ yAxisDefs: spec.yAxis ?? [], defaultFormat: format, hasDualAxis })}
        <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
        {spec.legend && <Legend wrapperStyle={{ fontSize: 11, marginTop: 8 }} />}
        {renderReferenceLines(spec.referenceLines, hasDualAxis)}
        {renderSeries(spec, palette)}
      </ComposedChart>
    )
  }

  if (spec.kind === 'horizontalBar') {
    return (
      <BarChart data={spec.data} layout="vertical" margin={{ top: 10, right: 16, left: 8, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} horizontal={false} />
        <XAxis type="number" tickFormatter={makeFormatter(format)} tick={AXIS_TICK_STYLE} axisLine={false} tickLine={false} />
        <YAxis type="category" dataKey={spec.xAxisKey} tick={AXIS_TICK_STYLE} axisLine={false} tickLine={false} width={120} />
        <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
        {spec.legend && <Legend wrapperStyle={{ fontSize: 11, marginTop: 8 }} />}
        {renderReferenceLines(spec.referenceLines, hasDualAxis)}
        {renderSeries(spec, palette, 'bar')}
      </BarChart>
    )
  }

  if (spec.kind === 'bar' || spec.kind === 'stacked') {
    return (
      <BarChart data={spec.data} margin={layout.margin}>
        <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
        <XAxis dataKey={spec.xAxisKey} tick={layout.xAxisTick} height={layout.xAxisHeight} interval="preserveStartEnd" axisLine={false} tickLine={false} />
        {renderYAxes({ yAxisDefs: spec.yAxis ?? [], defaultFormat: format, hasDualAxis })}
        <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
        {spec.legend && <Legend wrapperStyle={{ fontSize: 11, marginTop: 8 }} />}
        {renderReferenceLines(spec.referenceLines, hasDualAxis)}
        {renderSeries(spec, palette, 'bar')}
      </BarChart>
    )
  }

  if (spec.kind === 'area') {
    return (
      <AreaChart data={spec.data} margin={layout.margin}>
        {renderAreaDefs(spec.series, palette)}
        <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
        <XAxis dataKey={spec.xAxisKey} tick={layout.xAxisTick} height={layout.xAxisHeight} interval="preserveStartEnd" axisLine={false} tickLine={false} />
        {renderYAxes({ yAxisDefs: spec.yAxis ?? [], defaultFormat: format, hasDualAxis })}
        <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
        {spec.legend && <Legend wrapperStyle={{ fontSize: 11, marginTop: 8 }} />}
        {renderReferenceLines(spec.referenceLines, hasDualAxis)}
        {renderSeries(spec, palette, 'area')}
      </AreaChart>
    )
  }

  // Default: line chart
  return (
    <LineChart data={spec.data} margin={layout.margin}>
      <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
      <XAxis dataKey={spec.xAxisKey} tick={layout.xAxisTick} height={layout.xAxisHeight} interval="preserveStartEnd" axisLine={false} tickLine={false} />
      {renderYAxes({ yAxisDefs: spec.yAxis ?? [], defaultFormat: format, hasDualAxis })}
      <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
      {spec.legend && <Legend wrapperStyle={{ fontSize: 11, marginTop: 8 }} />}
      {renderReferenceLines(spec.referenceLines, hasDualAxis)}
      {renderSeries(spec, palette)}
    </LineChart>
  )
}

function PieLikeChart({ spec, palette }: { spec: ChartSpec; palette: string[] }) {
  const valueKey = spec.valueKey ?? 'value'
  const nameKey = spec.nameKey ?? 'name'
  const isDonut = spec.kind === 'donut'
  const total = spec.data.reduce((sum, row) => sum + (Number(row[valueKey]) || 0), 0)

  return (
    <PieChart>
      <Pie
        data={spec.data}
        cx="50%"
        cy="50%"
        innerRadius={isDonut ? 55 : 0}
        outerRadius={90}
        paddingAngle={isDonut ? 2 : 1}
        dataKey={valueKey}
        nameKey={nameKey}
      >
        {spec.data.map((row, i) => {
          const explicit = (row.color as string | undefined) ?? (row.fill as string | undefined)
          const nameKeyVal = String(row[nameKey] ?? '').toLowerCase()
          const color = explicit
            ? explicit
            : STATUS_COLORS[nameKeyVal]
              ? STATUS_COLORS[nameKeyVal]
              : palette[i % palette.length]
          return <Cell key={`cell-${i}`} fill={color} />
        })}
      </Pie>
      {isDonut && total > 0 && (
        <text x="50%" y="50%" textAnchor="middle" dominantBaseline="middle" className="fill-ink" style={{ fontSize: '18px', fontWeight: 700 }}>
          {makeFormatter(spec.formatValueAs)(total)}
        </text>
      )}
      <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v, name) => [makeFormatter(spec.formatValueAs)(Number(v)), String(name)]} />
      {spec.legend && <Legend wrapperStyle={{ fontSize: 11 }} />}
    </PieChart>
  )
}

function ScatterLikeChart({ spec, palette, hasDualAxis, layout }: CartesianProps) {
  const format = spec.formatValueAs
  // X field: prefer xAxisKey (consistent with other cartesian kinds the AI
  // already knows), fall back to xKey for back-compat. Y comes from series[].
  const xField = spec.xAxisKey ?? spec.xKey ?? 'x'
  const ySeries = spec.series?.length ? spec.series : [{ key: spec.yKey ?? 'y' }]
  return (
    <ScatterChart margin={layout.margin}>
      <CartesianGrid strokeDasharray="3 3" stroke={GRID_STROKE} />
      <XAxis
        type="number"
        dataKey={xField}
        name={xField}
        tickFormatter={makeFormatter(format)}
        tick={layout.xAxisTick}
        height={layout.xAxisHeight}
        axisLine={false}
        tickLine={false}
      />
      {renderYAxes({ yAxisDefs: spec.yAxis ?? [], defaultFormat: format, hasDualAxis })}
      <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
      {spec.legend && <Legend wrapperStyle={{ fontSize: 11, marginTop: 8 }} />}
      {renderReferenceLines(spec.referenceLines, hasDualAxis)}
      {ySeries.map((s, i) => (
        <Scatter
          key={s.key}
          dataKey={s.key}
          name={s.name ?? s.key}
          fill={resolveColor(s.color, palette, i, s.key)}
        />
      ))}
    </ScatterChart>
  )
}

function RadarLikeChart({ spec, palette }: { spec: ChartSpec; palette: string[] }) {
  const format = spec.formatValueAs
  return (
    <RadarChart data={spec.data} outerRadius="75%">
      <PolarGrid stroke={GRID_STROKE} />
      <PolarAngleAxis dataKey={spec.xAxisKey} tick={{ fontSize: 11, fill: '#6a7282' }} />
      <PolarRadiusAxis tickFormatter={makeFormatter(format)} tick={{ fontSize: 10, fill: '#6a7282' }} />
      {(spec.series ?? []).map((s, i) => (
        <Radar
          key={s.key}
          dataKey={s.key}
          name={s.name ?? s.key}
          stroke={resolveColor(s.color, palette, i, s.key)}
          fill={resolveColor(s.color, palette, i, s.key)}
          fillOpacity={0.2}
        />
      ))}
      <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [makeFormatter(format)(Number(v)), '']} />
      {spec.legend && <Legend wrapperStyle={{ fontSize: 11 }} />}
    </RadarChart>
  )
}

function RadialGaugeChart({ spec, palette }: { spec: ChartSpec; palette: string[] }) {
  const valueKey = spec.valueKey ?? 'value'
  const data = spec.data.map((row, i) => ({
    ...row,
    // Carry the display name through so Legend/Tooltip can read it, even though
    // Recharts v3's <RadialBar> no longer accepts a nameKey prop directly.
    name: row[spec.nameKey ?? 'name'] ?? row.name,
    fill: (row.color as string | undefined) ?? palette[i % palette.length],
  }))
  return (
    <RadialBarChart
      data={data}
      innerRadius="20%"
      outerRadius="100%"
      startAngle={90}
      endAngle={-270}
    >
      <RadialBar dataKey={valueKey} background />
      {spec.legend && <Legend wrapperStyle={{ fontSize: 11 }} iconSize={10} />}
      <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v, name) => [makeFormatter(spec.formatValueAs)(Number(v)), String(name)]} />
    </RadialBarChart>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Error fallback — shown when Zod parse fails or required fields are missing.
// Mirrors the MessageBubble error banner styling so reload-persisted bad data is
// always visible (never a silent blank).
// ─────────────────────────────────────────────────────────────────────────────
export function ChartError({ reason, raw }: { reason: string; raw: string }) {
  return (
    <div className="my-3 rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-800">
      <div className="flex items-center gap-1.5 font-semibold">
        <span>⚠️</span>
        <span>Lỗi render biểu đồ</span>
      </div>
      <p className="mt-1 opacity-80">{reason}</p>
      <pre className="mt-2 max-h-40 overflow-auto rounded-lg bg-red-100/60 p-2 font-mono text-[11px] leading-tight whitespace-pre-wrap break-all">
        {raw}
      </pre>
    </div>
  )
}

function getChartLayout(spec: ChartSpec): { height: number; minWidth: number; cartesian: CartesianLayout } {
  const pointCount = spec.data.length
  const seriesCount = spec.series?.length ?? 1
  const isCartesian = ['line', 'area', 'bar', 'horizontalBar', 'stacked', 'composed', 'scatter'].includes(spec.kind)
  const denseXAxis = isCartesian && pointCount > 8
  const dualAxis = (spec.yAxis?.length ?? 0) >= 2

  if (spec.kind === 'horizontalBar') {
    const height = spec.height ?? Math.min(560, Math.max(260, pointCount * 34 + 120))
    return {
      height,
      minWidth: 520,
      cartesian: {
        margin: { top: 12, right: 24, left: 10, bottom: 18 },
        xAxisHeight: 32,
        xAxisTick: AXIS_TICK_STYLE,
      },
    }
  }

  const height = spec.height ?? Math.min(560, Math.max(340, 290 + Math.max(0, seriesCount - 1) * 24 + Math.max(0, pointCount - 10) * 5))
  const minWidth = isCartesian ? Math.max(680, pointCount * (denseXAxis ? 48 : 38), dualAxis ? 760 : 0) : 360

  return {
    height,
    minWidth,
    cartesian: {
      margin: {
        top: 18,
        right: dualAxis ? 36 : 24,
        left: 12,
        bottom: denseXAxis ? 56 : 28,
      },
      xAxisHeight: denseXAxis ? 68 : 36,
      xAxisTick: denseXAxis
        ? { ...AXIS_TICK_STYLE, fontSize: 10, angle: -38, textAnchor: 'end' }
        : AXIS_TICK_STYLE,
    },
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Public component. Expects a Zod-parsed spec (caller validates).
// ─────────────────────────────────────────────────────────────────────────────
export function ChartBlock({ spec }: { spec: ChartSpec }) {
  const palette = spec.colors?.length ? spec.colors : CATEGORICAL_PALETTE
  const hasDualAxis = (spec.yAxis?.length ?? 0) >= 2
  const layout = getChartLayout(spec)

  // Validate required fields per kind; render error card if missing.
  const validationError = useMemo(() => {
    const cartesian = ['line', 'area', 'bar', 'horizontalBar', 'stacked', 'composed']
    if (cartesian.includes(spec.kind)) {
      if (!spec.xAxisKey) return `Thiếu xAxisKey cho loại biểu đồ "${spec.kind}".`
      if (!spec.series || spec.series.length === 0) return `Thiếu series cho loại biểu đồ "${spec.kind}".`
    }
    if (spec.kind === 'pie' || spec.kind === 'donut' || spec.kind === 'radialBar') {
      if (!spec.valueKey) return `Thiếu valueKey cho loại biểu đồ "${spec.kind}".`
    }
    if (spec.kind === 'scatter') {
      if (!spec.xAxisKey && !spec.xKey) return 'Thiếu xAxisKey (hoặc xKey) cho biểu đồ scatter.'
      if (!spec.series || spec.series.length === 0) return 'Thiếu series cho biểu đồ scatter.'
    }
    if (spec.kind === 'radar') {
      if (!spec.xAxisKey) return 'Thiếu xAxisKey cho biểu đồ radar.'
      if (!spec.series || spec.series.length === 0) return 'Thiếu series cho biểu đồ radar.'
    }
    if (!spec.data || spec.data.length === 0) return 'Không có dữ liệu (data rỗng).'
    return null
  }, [spec])

  if (validationError) {
    return <ChartError reason={validationError} raw={JSON.stringify(spec, null, 2)} />
  }

  let chartEl: React.ReactNode
  switch (spec.kind) {
    case 'pie':
    case 'donut':
      chartEl = <PieLikeChart spec={spec} palette={palette} />
      break
    case 'scatter':
      chartEl = <ScatterLikeChart spec={spec} palette={palette} hasDualAxis={hasDualAxis} layout={layout.cartesian} />
      break
    case 'radar':
      chartEl = <RadarLikeChart spec={spec} palette={palette} />
      break
    case 'radialBar':
      chartEl = <RadialGaugeChart spec={spec} palette={palette} />
      break
    default:
      chartEl = <CartesianChart spec={spec} palette={palette} hasDualAxis={hasDualAxis} layout={layout.cartesian} />
  }

  return (
    <div className="my-3 w-full rounded-xl border border-border bg-white p-4 shadow-xs">
      {(spec.title || spec.subtitle) && (
        <div className="mb-3">
          {spec.title && <h4 className="text-sm font-semibold text-ink">{spec.title}</h4>}
          {spec.subtitle && <p className="mt-0.5 text-xs text-muted-foreground">{spec.subtitle}</p>}
        </div>
      )}
      <div className="max-w-full overflow-x-auto overscroll-x-contain px-2 pb-5">
        <div style={{ height: layout.height, minWidth: layout.minWidth }}>
          <ResponsiveContainer width="100%" height="100%" minWidth={layout.minWidth}>
            {chartEl}
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  )
}
