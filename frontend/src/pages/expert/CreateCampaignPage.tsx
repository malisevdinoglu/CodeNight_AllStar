import { useState, type FormEvent } from 'react'
import toast from 'react-hot-toast'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import type { CampaignType, Priority, Segment } from '../../api/types'
import { SegmentBadge } from '../../components/domain'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui'
import { useCreateCampaign } from '../../hooks/useCampaign'

const campaignSchema = z.object({
  title: z.string().min(5, 'Kampanya başlığı en az 5 karakter olmalı.'),
  type: z.enum(['EK_PAKET', 'TARIFE_YUKSELTME', 'CIHAZ_FIRSATI', 'SADAKAT']),
  targetSegment: z.enum(['YUKSEK_DEGER', 'RISKLI_KAYIP', 'YENI_ABONE', 'PASIF', 'BELIRSIZ']),
  description: z.string().min(10, 'Açıklama en az 10 karakter olmalı.'),
})

const campaignTypes: CampaignType[] = ['EK_PAKET', 'TARIFE_YUKSELTME', 'CIHAZ_FIRSATI', 'SADAKAT']
const segments: Segment[] = ['RISKLI_KAYIP', 'YUKSEK_DEGER', 'YENI_ABONE', 'PASIF', 'BELIRSIZ']

const campaignTypeLabels: Record<CampaignType, string> = {
  EK_PAKET: 'Ek paket',
  TARIFE_YUKSELTME: 'Tarife yükseltme',
  CIHAZ_FIRSATI: 'Cihaz fırsatı',
  SADAKAT: 'Sadakat',
}

const segmentLabels: Record<Segment, string> = {
  RISKLI_KAYIP: 'Riskli kayıp',
  YUKSEK_DEGER: 'Yüksek değer',
  YENI_ABONE: 'Yeni abone',
  PASIF: 'Pasif',
  BELIRSIZ: 'Belirsiz',
}

const priorityLabels: Record<Priority, string> = {
  KRITIK: 'Kritik',
  YUKSEK: 'Yüksek',
  ORTA: 'Orta',
  DUSUK: 'Düşük',
}

export function CreateCampaignPage() {
  const createCampaign = useCreateCampaign()
  const [title, setTitle] = useState('Riskli kayıp aboneleri geri kazanma')
  const [type, setType] = useState<CampaignType>('EK_PAKET')
  const [targetSegment, setTargetSegment] = useState<Segment>('RISKLI_KAYIP')
  const [description, setDescription] = useState(
    'Son 30 günde kullanım düşüşü olan abonelere yüksek etkili ek paket teklifi.',
  )
  const [errors, setErrors] = useState<string[]>([])

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const parsed = campaignSchema.safeParse({ title, type, targetSegment, description })

    if (!parsed.success) {
      setErrors(parsed.error.issues.map((issue) => issue.message))
      return
    }

    setErrors([])
    createCampaign.mutate(parsed.data, {
      onSuccess: (result) => {
        toast.success('Kampanya oluşturuldu, vaka akışa alındı.')
        if (!result.aiAvailable) {
          toast('Kampanya manuel inceleme kuyruğuna alındı.')
        }
      },
    })
  }

  return (
    <section className="grid gap-6 xl:grid-cols-[1fr_24rem]">
      <Card className="overflow-hidden border-blue-100 shadow-lg shadow-blue-950/5">
        <CardHeader className="border-blue-100 bg-[#294b98] text-white">
          <CardTitle className="text-xl text-white">Yeni Kampanya Oluştur</CardTitle>
          <CardDescription className="text-white/72">
            Hedef kitle, kampanya tipi ve açıklamayı girerek kampanya akışına başla.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {errors.length > 0 ? (
            <div className="mb-5 rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <p className="font-bold">Form kontrolü</p>
              <ul className="mt-2 list-disc space-y-1 pl-5">
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </div>
          ) : null}

          <form className="grid gap-5" onSubmit={handleSubmit}>
            <label className="block">
              <span className="text-sm font-semibold text-slate-700">Kampanya başlığı</span>
              <input
                className="mt-2 h-11 w-full rounded-md border border-blue-100 bg-blue-50/40 px-3 text-sm outline-none transition focus:border-brand-navy focus:bg-white focus:ring-2 focus:ring-brand-navy/15"
                onChange={(event) => setTitle(event.target.value)}
                value={title}
              />
            </label>

            <div className="grid gap-5 md:grid-cols-2">
              <label className="block">
                <span className="text-sm font-semibold text-slate-700">Kampanya tipi</span>
                <select
                  className="mt-2 h-11 w-full rounded-md border border-blue-100 bg-blue-50/40 px-3 text-sm outline-none transition focus:border-brand-navy focus:bg-white focus:ring-2 focus:ring-brand-navy/15"
                  onChange={(event) => setType(event.target.value as CampaignType)}
                  value={type}
                >
                  {campaignTypes.map((item) => (
                    <option key={item} value={item}>
                      {campaignTypeLabels[item]}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block">
                <span className="text-sm font-semibold text-slate-700">Hedef segment</span>
                <select
                  className="mt-2 h-11 w-full rounded-md border border-blue-100 bg-blue-50/40 px-3 text-sm outline-none transition focus:border-brand-navy focus:bg-white focus:ring-2 focus:ring-brand-navy/15"
                  onChange={(event) => setTargetSegment(event.target.value as Segment)}
                  value={targetSegment}
                >
                  {segments.map((item) => (
                    <option key={item} value={item}>
                      {segmentLabels[item]}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <label className="block">
              <span className="text-sm font-semibold text-slate-700">Açıklama</span>
              <textarea
                className="mt-2 min-h-32 w-full rounded-md border border-blue-100 bg-blue-50/40 px-3 py-3 text-sm outline-none transition focus:border-brand-navy focus:bg-white focus:ring-2 focus:ring-brand-navy/15"
                onChange={(event) => setDescription(event.target.value)}
                value={description}
              />
            </label>

            <Button
              className="bg-brand-yellow text-brand-navy hover:bg-yellow-300 focus-visible:ring-brand-yellow/30"
              isLoading={createCampaign.isPending}
              type="submit"
            >
              Kampanyayı Oluştur
            </Button>
          </form>
        </CardContent>
      </Card>

      <div className="space-y-6">
        <Card className="border-blue-100 shadow-lg shadow-blue-950/5">
          <CardHeader>
            <CardTitle>Öneri Özeti</CardTitle>
            <CardDescription>Son kampanya için oluşan öncelik ve vaka bilgisi.</CardDescription>
          </CardHeader>
          <CardContent>
            {createCampaign.data ? (
              <div className="space-y-4">
                <div className="flex flex-wrap items-center gap-2">
                  <SegmentBadge segment={createCampaign.data.predictedSegment} />
                  <Badge tone={createCampaign.data.aiAvailable ? 'success' : 'warning'}>
                    {createCampaign.data.aiAvailable ? 'Otomatik değerlendirme' : 'Manuel inceleme'}
                  </Badge>
                </div>
                <div className="rounded-md bg-blue-50 p-4">
                  <p className="text-xs font-bold uppercase text-slate-500">Vaka</p>
                  <p className="mt-1 text-lg font-bold text-slate-950">
                    {createCampaign.data.caseNumber}
                  </p>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="rounded-md border border-blue-100 p-3">
                    <p className="text-xs font-bold uppercase text-slate-500">Öncelik</p>
                    <p className="mt-1 font-bold text-slate-950">
                      {priorityLabels[createCampaign.data.priority]}
                    </p>
                  </div>
                  <div className="rounded-md border border-blue-100 p-3">
                    <p className="text-xs font-bold uppercase text-slate-500">Dönüşüm</p>
                    <p className="mt-1 font-bold text-slate-950">
                      {createCampaign.data.conversionProbability
                        ? `%${Math.round(createCampaign.data.conversionProbability * 100)}`
                        : 'Bekleniyor'}
                    </p>
                  </div>
                </div>
                <Link
                  className="inline-flex h-10 items-center justify-center rounded-md bg-brand-yellow px-4 text-sm font-bold text-brand-navy transition hover:bg-yellow-300"
                  to={`/cases/${createCampaign.data.caseId}`}
                >
                  Vakayı Aç
                </Link>
              </div>
            ) : (
              <p className="text-sm leading-6 text-slate-600">
                Kampanya oluşturulduğunda segment, öncelik ve oluşan vaka burada görünür.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </section>
  )
}
