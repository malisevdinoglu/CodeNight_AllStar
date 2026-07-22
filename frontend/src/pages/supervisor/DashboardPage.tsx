import { AlertTriangle, BrainCircuit, Clock3, UsersRound } from 'lucide-react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { Link } from 'react-router-dom'
import { PriorityBadge, SegmentBadge } from '../../components/domain'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  Spinner,
  Table,
  type DataTableColumn,
} from '../../components/ui'
import { useDashboardSummary } from '../../hooks/useDashboard'
import type { CaseDto } from '../../api/types'

const segmentColors = ['#00457C', '#FFC900', '#0f766e', '#64748b', '#ea580c']

const breachedColumns: DataTableColumn<CaseDto>[] = [
  {
    key: 'caseNumber',
    header: 'Vaka',
    render: (item) => <span className="font-bold text-slate-950">{item.caseNumber}</span>,
  },
  {
    key: 'segment',
    header: 'Segment',
    render: (item) => <SegmentBadge segment={item.segment} />,
  },
  {
    key: 'priority',
    header: 'Oncelik',
    render: (item) => <PriorityBadge priority={item.priority} />,
  },
]

export function DashboardPage() {
  const dashboardQuery = useDashboardSummary()

  if (dashboardQuery.isLoading) {
    return <Spinner className="min-h-80" label="Dashboard yukleniyor" />
  }

  if (dashboardQuery.isError) {
    return <ErrorState onRetry={() => dashboardQuery.refetch()} title="Dashboard alinamadi" />
  }

  if (!dashboardQuery.data) {
    return (
      <EmptyState
        description="Dashboard verisi backend hazir olunca bu alanda gorunecek."
        title="Veri bulunamadi"
      />
    )
  }

  const summary = dashboardQuery.data

  return (
    <section className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          icon={Clock3}
          label="SLA uyumu"
          tone="brand"
          value={`%${summary.slaComplianceRate.toFixed(1)}`}
        />
        <MetricCard
          icon={BrainCircuit}
          label="AI dogruluk"
          tone="info"
          value={`%${summary.aiAccuracy.overall.toFixed(1)}`}
        />
        <MetricCard
          icon={AlertTriangle}
          label="SLA asan aktif vaka"
          tone="danger"
          value={summary.slaBreachedActiveCases.length.toString()}
        />
        <MetricCard
          icon={UsersRound}
          label="Bekleyen kuyruk"
          tone="warning"
          value={summary.pendingQueue.length.toString()}
        />
      </div>

      {summary.slaBreachedActiveCases.length > 0 ? (
        <Card className="border-red-200 bg-red-50">
          <CardHeader>
            <CardTitle>SLA asan vakalar</CardTitle>
            <CardDescription>Juri demosunda riskli operasyon sinyali en ustte gorunur.</CardDescription>
          </CardHeader>
          <CardContent>
            <Table
              columns={breachedColumns}
              getRowKey={(item) => item.id}
              items={summary.slaBreachedActiveCases}
            />
          </CardContent>
        </Card>
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[1fr_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Segment dagilimi</CardTitle>
            <CardDescription>Aktif kampanya vakalarinin segment kirilimi.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-72">
              <ResponsiveContainer height="100%" width="100%">
                <PieChart>
                  <Pie
                    data={summary.segmentDistribution}
                    dataKey="count"
                    innerRadius={60}
                    nameKey="segment"
                    outerRadius={95}
                    paddingAngle={2}
                  >
                    {summary.segmentDistribution.map((item, index) => (
                      <Cell fill={segmentColors[index % segmentColors.length]} key={item.segment} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Donusum trendi</CardTitle>
            <CardDescription>Son 7 gunluk kampanya donusum orani.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-72">
              <ResponsiveContainer height="100%" width="100%">
                <LineChart data={summary.conversionTrend}>
                  <CartesianGrid stroke="#e2e8f0" strokeDasharray="4 4" />
                  <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip />
                  <Line
                    dataKey="rate"
                    dot={{ fill: '#FFC900', strokeWidth: 2 }}
                    stroke="#00457C"
                    strokeWidth={3}
                    type="monotone"
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1fr_24rem]">
        <Card>
          <CardHeader>
            <CardTitle>Uzman performansi</CardTitle>
            <CardDescription>Tamamlanan vaka, ortalama lift ve sure etkisi.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-72">
              <ResponsiveContainer height="100%" width="100%">
                <BarChart data={summary.expertPerformance}>
                  <CartesianGrid stroke="#e2e8f0" strokeDasharray="4 4" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip />
                  <Bar dataKey="completedCount" fill="#00457C" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="avgLift" fill="#FFC900" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader
            action={
              <Link to="/queue">
                <Button variant="secondary">Kuyrugu ac</Button>
              </Link>
            }
          >
            <CardTitle>AI dogruluk</CardTitle>
            <CardDescription>Bonus ekrani icin kategori kirilimi.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {summary.aiAccuracy.byCategory.map((item) => (
                <div className="rounded-md border border-slate-200 p-3" key={item.segment}>
                  <div className="flex items-center justify-between gap-3">
                    <SegmentBadge segment={item.segment} />
                    <span className="text-sm font-bold text-brand-navy">
                      %{item.accuracy.toFixed(1)}
                    </span>
                  </div>
                  <div className="mt-3 h-2 rounded-full bg-slate-100">
                    <div
                      className="h-2 rounded-full bg-brand-navy"
                      style={{ width: `${item.accuracy}%` }}
                    />
                  </div>
                  <p className="mt-2 text-xs text-slate-500">{item.total} ornek</p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </section>
  )
}

type MetricCardProps = {
  icon: typeof Clock3
  label: string
  value: string
  tone: 'brand' | 'danger' | 'info' | 'warning'
}

function MetricCard({ icon: Icon, label, tone, value }: MetricCardProps) {
  const toneClass = {
    brand: 'bg-brand-yellow/20 text-brand-navy',
    danger: 'bg-red-50 text-red-700',
    info: 'bg-sky-50 text-sky-700',
    warning: 'bg-amber-50 text-amber-700',
  }[tone]

  return (
    <Card>
      <CardContent>
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
            <p className="mt-2 text-3xl font-bold text-slate-950">{value}</p>
          </div>
          <div className={`flex size-12 items-center justify-center rounded-md ${toneClass}`}>
            <Icon size={24} aria-hidden="true" />
          </div>
        </div>
        <Badge className="mt-4" tone={tone === 'danger' ? 'danger' : 'neutral'}>
          Backend/DB geldikten sonra canli veri
        </Badge>
      </CardContent>
    </Card>
  )
}
