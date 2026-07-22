import { ArrowLeft, Bot, CheckCircle2 } from 'lucide-react'
import { useState } from 'react'
import toast from 'react-hot-toast'
import { Link, useParams } from 'react-router-dom'
import type { CaseStatus, Segment } from '../../api/types'
import { getApiError } from '../../api/errors'
import { PriorityBadge, SegmentBadge, SlaCountdown } from '../../components/domain'
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
  Modal,
  Spinner,
} from '../../components/ui'
import {
  useCaseDetail,
  useOverrideSegment,
  useUpdateCaseStatus,
} from '../../hooks/useCases'

const stateFlow: CaseStatus[] = [
  'YENI',
  'ATANDI',
  'OPTIMIZE_EDILIYOR',
  'TEST_EDILIYOR',
  'TAMAMLANDI',
  'YAYINDA',
  'ARSIVLENDI',
]

const segments: Segment[] = ['RISKLI_KAYIP', 'YUKSEK_DEGER', 'YENI_ABONE', 'PASIF', 'BELIRSIZ']

const statusLabels: Record<CaseStatus, string> = {
  YENI: 'Yeni',
  ATANDI: 'Atandı',
  OPTIMIZE_EDILIYOR: 'Optimize ediliyor',
  TEST_EDILIYOR: 'Test ediliyor',
  TAMAMLANDI: 'Tamamlandı',
  YAYINDA: 'Yayında',
  ARSIVLENDI: 'Arşivlendi',
}

const segmentLabels: Record<Segment, string> = {
  RISKLI_KAYIP: 'Riskli kayıp',
  YUKSEK_DEGER: 'Yüksek değer',
  YENI_ABONE: 'Yeni abone',
  PASIF: 'Pasif',
  BELIRSIZ: 'Belirsiz',
}

export function CaseDetailPage() {
  const { caseId } = useParams()
  const caseQuery = useCaseDetail(caseId)
  const updateStatus = useUpdateCaseStatus(caseId ?? '')
  const overrideSegment = useOverrideSegment(caseId ?? '')
  const [targetStatus, setTargetStatus] = useState<CaseStatus | null>(null)
  const [note, setNote] = useState('')

  const selectedCase = caseQuery.data

  const handleStatusClick = (status: CaseStatus) => {
    if (status === 'TAMAMLANDI') {
      setTargetStatus(status)
      return
    }

    updateStatus.mutate(
      { targetStatus: status },
      {
        onSuccess: () => toast.success('Vaka durumu güncellendi.'),
        onError: (error) => toast.error(getApiError(error).message),
      },
    )
  }

  const handleComplete = () => {
    if (!targetStatus) {
      return
    }

    updateStatus.mutate(
      { targetStatus, note },
      {
        onSuccess: () => {
          toast.success('Vaka tamamlandı.')
          setTargetStatus(null)
          setNote('')
        },
        onError: (error) => toast.error(getApiError(error).message),
      },
    )
  }

  if (caseQuery.isLoading) {
    return <Spinner className="min-h-80" label="Vaka detayı yükleniyor" />
  }

  if (caseQuery.isError) {
    return <ErrorState onRetry={() => caseQuery.refetch()} title="Vaka detayı alınamadı" />
  }

  if (!selectedCase) {
    return (
      <EmptyState
        description="Seçilen vaka şu anda görüntülenemiyor."
        title="Vaka bulunamadı"
      />
    )
  }

  return (
    <section className="space-y-6">
      <Link
        className="inline-flex items-center gap-2 text-sm font-bold text-brand-navy hover:text-brand-ink"
        to="/cases"
      >
        <ArrowLeft size={17} aria-hidden="true" />
        Vakalara dön
      </Link>

      <Card className="overflow-hidden border-blue-100 shadow-lg shadow-blue-950/5">
        <CardHeader className="border-blue-100 bg-[#294b98] text-white">
          <CardTitle className="text-xl text-white">{selectedCase.campaignTitle}</CardTitle>
          <CardDescription className="text-white/72">{selectedCase.caseNumber}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap items-center gap-2">
            <SegmentBadge segment={selectedCase.segment} />
            <PriorityBadge priority={selectedCase.priority} />
            <Badge tone="neutral">{statusLabels[selectedCase.status]}</Badge>
            <SlaCountdown
              breached={selectedCase.slaBreached}
              remainingSeconds={selectedCase.remainingSlaSeconds}
            />
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 xl:grid-cols-[1fr_22rem]">
        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Durum Akışı</CardTitle>
            <CardDescription>
              Vaka için kullanılabilir sonraki adımları buradan yönetebilirsin.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 md:grid-cols-7">
              {stateFlow.map((status) => {
                const isCurrent = selectedCase.status === status
                const isAllowed = selectedCase.allowedTransitions.includes(status)

                return (
                    <div
                      className={`rounded-md border p-3 text-center ${
                        isCurrent
                        ? 'border-brand-yellow bg-brand-yellow/20'
                        : isAllowed
                          ? 'border-brand-navy/30 bg-blue-50'
                          : 'border-blue-100 bg-white'
                      }`}
                      key={status}
                  >
                    <p className="text-xs font-bold text-slate-700">{statusLabels[status]}</p>
                    {isCurrent ? (
                      <CheckCircle2 className="mx-auto mt-2 text-brand-navy" size={18} />
                    ) : null}
                  </div>
                )
              })}
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              {selectedCase.allowedTransitions.length > 0 ? (
                selectedCase.allowedTransitions.map((status) => (
                  <Button
                    isLoading={updateStatus.isPending}
                    key={status}
                    onClick={() => handleStatusClick(status)}
                  >
                    {statusLabels[status]} Yap
                  </Button>
                ))
              ) : (
                <Badge tone="neutral">Bu durumda kullanılabilir aksiyon yok</Badge>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Tahmin Özeti</CardTitle>
            <CardDescription>Segment ve dönüşüm olasılığı.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="rounded-md border border-brand-yellow/60 bg-brand-yellow/15 p-4">
              <div className="flex items-center gap-2 text-sm font-bold text-brand-navy">
                <Bot size={18} aria-hidden="true" />
                Akıllı öneri
              </div>
              <p className="mt-3 text-2xl font-bold text-slate-950">
                {selectedCase.conversionProbability
                  ? `%${Math.round(selectedCase.conversionProbability * 100)}`
                  : 'Bekleniyor'}
              </p>
              <p className="mt-1 text-sm text-slate-600">Dönüşüm olasılığı</p>
            </div>

            <label className="mt-5 block">
              <span className="text-sm font-semibold text-slate-700">Segment düzeltme</span>
              <select
                className="mt-2 h-11 w-full rounded-md border border-blue-100 bg-blue-50/40 px-3 text-sm outline-none transition focus:border-brand-navy focus:bg-white focus:ring-2 focus:ring-brand-navy/15"
                disabled={overrideSegment.isPending}
                onChange={(event) =>
                  overrideSegment.mutate(
                    { segment: event.target.value as Segment },
                    {
                      onSuccess: () => toast.success('Segment güncellendi.'),
                      onError: (error) => toast.error(getApiError(error).message),
                    },
                  )
                }
                value={selectedCase.segment}
              >
                {segments.map((segment) => (
                  <option key={segment} value={segment}>
                    {segmentLabels[segment]}
                  </option>
                ))}
              </select>
            </label>
          </CardContent>
        </Card>
      </div>

      <Modal
        description="Tamamlandı durumuna geçmek için uzman notu zorunludur."
        footer={
          <>
            <Button onClick={() => setTargetStatus(null)} variant="secondary">
              Vazgeç
            </Button>
            <Button isLoading={updateStatus.isPending} onClick={handleComplete}>
              Tamamla
            </Button>
          </>
        }
        isOpen={targetStatus === 'TAMAMLANDI'}
        onClose={() => setTargetStatus(null)}
        title="Uzman notu"
      >
        <textarea
          className="min-h-32 w-full rounded-md border border-blue-100 bg-blue-50/40 px-3 py-3 text-sm outline-none transition focus:border-brand-navy focus:bg-white focus:ring-2 focus:ring-brand-navy/15"
          onChange={(event) => setNote(event.target.value)}
          placeholder="Optimizasyon notunu yazın"
          value={note}
        />
      </Modal>
    </section>
  )
}
