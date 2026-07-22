import { UserCheck } from 'lucide-react'
import { useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import type { CaseDto } from '../../api/types'
import { getApiError } from '../../api/errors'
import { PriorityBadge, SegmentBadge, SlaCountdown } from '../../components/domain'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  Modal,
  Spinner,
  Table,
  type DataTableColumn,
} from '../../components/ui'
import { useAssignCase } from '../../hooks/useCases'
import { useDashboardSummary } from '../../hooks/useDashboard'

const fallbackExperts = [
  { expertId: 'expert-001', name: 'Osman Erkan' },
  { expertId: 'expert-002', name: 'Mali Demir' },
  { expertId: 'expert-003', name: 'Iskender Aksoy' },
]

export function QueuePage() {
  const dashboardQuery = useDashboardSummary()
  const [selectedCase, setSelectedCase] = useState<CaseDto | null>(null)
  const [expertId, setExpertId] = useState('expert-001')
  const assignCase = useAssignCase(selectedCase?.id ?? '')
  const experts = dashboardQuery.data?.expertPerformance ?? fallbackExperts

  const columns = useMemo<DataTableColumn<CaseDto>[]>(
    () => [
      {
        key: 'caseNumber',
        header: 'Vaka',
        render: (item) => <span className="font-bold text-slate-950">{item.caseNumber}</span>,
      },
      {
        key: 'campaignTitle',
        header: 'Kampanya',
        render: (item) => <span className="font-semibold text-slate-800">{item.campaignTitle}</span>,
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
      {
        key: 'sla',
        header: 'SLA',
        render: (item) => (
          <SlaCountdown breached={item.slaBreached} remainingSeconds={item.remainingSlaSeconds} />
        ),
      },
      {
        key: 'action',
        header: 'Aksiyon',
        align: 'right',
        render: (item) => (
          <Button
            leftIcon={<UserCheck size={17} aria-hidden="true" />}
            onClick={() => {
              setSelectedCase(item)
              setExpertId(experts[0]?.expertId ?? 'expert-001')
            }}
            size="sm"
          >
            Ata
          </Button>
        ),
      },
    ],
    [experts],
  )

  const handleAssign = () => {
    if (!selectedCase) {
      return
    }

    assignCase.mutate(
      { expertId },
      {
        onSuccess: () => {
          toast.success('Vaka secilen uzmana atandi.')
          setSelectedCase(null)
        },
        onError: (error) => toast.error(getApiError(error).message),
      },
    )
  }

  if (dashboardQuery.isLoading) {
    return <Spinner className="min-h-80" label="Kuyruk yukleniyor" />
  }

  if (dashboardQuery.isError) {
    return <ErrorState onRetry={() => dashboardQuery.refetch()} title="Kuyruk alinamadi" />
  }

  const pendingQueue = dashboardQuery.data?.pendingQueue ?? []

  return (
    <section className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Manuel atama kuyrugu</CardTitle>
          <CardDescription>
            AI fallback, BELIRSIZ segment veya kapasite bekleyen vakalar buradan atanir.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {pendingQueue.length === 0 ? (
            <EmptyState
              description="Su anda manuel atama bekleyen vaka yok."
              title="Kuyruk temiz"
            />
          ) : (
            <Table columns={columns} getRowKey={(item) => item.id} items={pendingQueue} />
          )}
        </CardContent>
      </Card>

      <Modal
        description="Atama tamamlandiginda vaka kuyruktan cikar ve ilgili uzmana duser."
        footer={
          <>
            <Button onClick={() => setSelectedCase(null)} variant="secondary">
              Vazgec
            </Button>
            <Button isLoading={assignCase.isPending} onClick={handleAssign}>
              Ata
            </Button>
          </>
        }
        isOpen={Boolean(selectedCase)}
        onClose={() => setSelectedCase(null)}
        title="Uzman ata"
      >
        <div className="space-y-4">
          <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
            <p className="text-xs font-bold uppercase text-slate-500">Secilen vaka</p>
            <p className="mt-1 text-sm font-bold text-slate-950">{selectedCase?.caseNumber}</p>
          </div>
          <label className="block">
            <span className="text-sm font-semibold text-slate-700">Uzman</span>
            <select
              className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
              onChange={(event) => setExpertId(event.target.value)}
              value={expertId}
            >
              {experts.map((expert) => (
                <option key={expert.expertId} value={expert.expertId}>
                  {expert.name}
                </option>
              ))}
            </select>
          </label>
        </div>
      </Modal>
    </section>
  )
}
