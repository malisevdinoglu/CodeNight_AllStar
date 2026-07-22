import { useState, type FormEvent } from 'react'
import toast from 'react-hot-toast'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import type { CampaignType, Segment } from '../../api/types'
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
  title: z.string().min(5, 'Kampanya basligi en az 5 karakter olmali.'),
  type: z.enum(['EK_PAKET', 'TARIFE_YUKSELTME', 'CIHAZ_FIRSATI', 'SADAKAT']),
  targetSegment: z.enum(['YUKSEK_DEGER', 'RISKLI_KAYIP', 'YENI_ABONE', 'PASIF', 'BELIRSIZ']),
  description: z.string().min(10, 'Aciklama en az 10 karakter olmali.'),
})

const campaignTypes: CampaignType[] = ['EK_PAKET', 'TARIFE_YUKSELTME', 'CIHAZ_FIRSATI', 'SADAKAT']
const segments: Segment[] = ['RISKLI_KAYIP', 'YUKSEK_DEGER', 'YENI_ABONE', 'PASIF', 'BELIRSIZ']

export function CreateCampaignPage() {
  const createCampaign = useCreateCampaign()
  const [title, setTitle] = useState('Riskli kayip aboneleri geri kazanma')
  const [type, setType] = useState<CampaignType>('EK_PAKET')
  const [targetSegment, setTargetSegment] = useState<Segment>('RISKLI_KAYIP')
  const [description, setDescription] = useState(
    'Son 30 gunde kullanim dususu olan abonelere yuksek etkili ek paket teklifi.',
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
        toast.success('Kampanya olusturuldu, vaka akisa alindi.')
        if (!result.aiAvailable) {
          toast('AI degerlendirmesi bekleniyor, manuel kuyruga alindi.')
        }
      },
    })
  }

  return (
    <section className="grid gap-6 xl:grid-cols-[1fr_24rem]">
      <Card>
        <CardHeader>
          <CardTitle>Yeni kampanya olustur</CardTitle>
          <CardDescription>
            Backend gelene kadar MSW, Campaign Service ve AI fallback sozlesmesini simule eder.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {errors.length > 0 ? (
            <div className="mb-5 rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              <p className="font-bold">Form kontrolu</p>
              <ul className="mt-2 list-disc space-y-1 pl-5">
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </div>
          ) : null}

          <form className="grid gap-5" onSubmit={handleSubmit}>
            <label className="block">
              <span className="text-sm font-semibold text-slate-700">Kampanya basligi</span>
              <input
                className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                onChange={(event) => setTitle(event.target.value)}
                value={title}
              />
            </label>

            <div className="grid gap-5 md:grid-cols-2">
              <label className="block">
                <span className="text-sm font-semibold text-slate-700">Kampanya tipi</span>
                <select
                  className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                  onChange={(event) => setType(event.target.value as CampaignType)}
                  value={type}
                >
                  {campaignTypes.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block">
                <span className="text-sm font-semibold text-slate-700">Hedef segment</span>
                <select
                  className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                  onChange={(event) => setTargetSegment(event.target.value as Segment)}
                  value={targetSegment}
                >
                  {segments.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <label className="block">
              <span className="text-sm font-semibold text-slate-700">Aciklama</span>
              <textarea
                className="mt-2 min-h-32 w-full rounded-md border border-slate-300 px-3 py-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                onChange={(event) => setDescription(event.target.value)}
                value={description}
              />
            </label>

            <Button isLoading={createCampaign.isPending} type="submit">
              Kampanyayi olustur
            </Button>
          </form>
        </CardContent>
      </Card>

      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>AI sonucu</CardTitle>
            <CardDescription>Son olusturma isteginin demo ciktisi.</CardDescription>
          </CardHeader>
          <CardContent>
            {createCampaign.data ? (
              <div className="space-y-4">
                <div className="flex flex-wrap items-center gap-2">
                  <SegmentBadge segment={createCampaign.data.predictedSegment} />
                  <Badge tone={createCampaign.data.aiAvailable ? 'success' : 'warning'}>
                    {createCampaign.data.aiAvailable ? 'AI aktif' : 'Manuel kuyruk'}
                  </Badge>
                </div>
                <div className="rounded-md bg-slate-50 p-4">
                  <p className="text-xs font-bold uppercase text-slate-500">Vaka</p>
                  <p className="mt-1 text-lg font-bold text-slate-950">
                    {createCampaign.data.caseNumber}
                  </p>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="rounded-md border border-slate-200 p-3">
                    <p className="text-xs font-bold uppercase text-slate-500">Oncelik</p>
                    <p className="mt-1 font-bold text-slate-950">{createCampaign.data.priority}</p>
                  </div>
                  <div className="rounded-md border border-slate-200 p-3">
                    <p className="text-xs font-bold uppercase text-slate-500">Donusum</p>
                    <p className="mt-1 font-bold text-slate-950">
                      {createCampaign.data.conversionProbability
                        ? `%${Math.round(createCampaign.data.conversionProbability * 100)}`
                        : 'Bekleniyor'}
                    </p>
                  </div>
                </div>
                <Link
                  className="inline-flex h-10 items-center justify-center rounded-md bg-brand-navy px-4 text-sm font-bold text-white transition hover:bg-brand-ink"
                  to={`/cases/${createCampaign.data.caseId}`}
                >
                  Vakayi ac
                </Link>
              </div>
            ) : (
              <p className="text-sm leading-6 text-slate-600">
                Kampanya olusturuldugunda AI segmenti, oncelik ve olusan vaka burada gorunur.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </section>
  )
}
