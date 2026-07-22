import { useMutation } from '@tanstack/react-query'
import {
  ArrowRight,
  BadgeCheck,
  KeyRound,
  Loader2,
  LockKeyhole,
  Phone,
  Shield,
} from 'lucide-react'
import { type FormEvent, useState } from 'react'
import toast from 'react-hot-toast'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { authApi } from '../../api'
import { getApiError } from '../../api/errors'
import { getRoleHomePath } from '../../app/roleRoutes'
import { useAuthStore } from '../../stores/auth.store'

type LoginMode = 'staff' | 'subscriber'

const staffLoginSchema = z.object({
  email: z.string().email('Gecerli bir e-posta girin.'),
  password: z.string().min(1, 'Sifre zorunludur.'),
})

const otpRequestSchema = z.object({
  gsmNumber: z
    .string()
    .min(10, 'GSM numarasi en az 10 haneli olmali.')
    .regex(/^[0-9+ ]+$/, 'GSM numarasi sadece rakam, bosluk veya + icerebilir.'),
})

const otpVerifySchema = otpRequestSchema.extend({
  otpCode: z.string().length(4, 'OTP kodu 4 haneli olmali.'),
})

const demoAccounts = [
  'personel@campaigncell.local',
  'supervisor@campaigncell.local',
  'admin@campaigncell.local',
] as const

export function LoginPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((state) => state.setSession)
  const [mode, setMode] = useState<LoginMode>('staff')
  const [email, setEmail] = useState('personel@campaigncell.local')
  const [password, setPassword] = useState('Password1')
  const [gsmNumber, setGsmNumber] = useState('5321112233')
  const [otpCode, setOtpCode] = useState('1234')
  const [otpRequested, setOtpRequested] = useState(false)
  const [formErrors, setFormErrors] = useState<string[]>([])

  const completeLogin = (session: Awaited<ReturnType<typeof authApi.login>>) => {
    setSession(session)
    toast.success(`${session.user.firstName} ${session.user.lastName} giris yapti.`)
    navigate(getRoleHomePath(session.user.role), { replace: true })
  }

  const staffLogin = useMutation({
    mutationFn: authApi.login,
    onSuccess: completeLogin,
    onError: (error) => {
      const apiError = getApiError(error)
      setFormErrors(apiError.details?.length ? apiError.details : [apiError.message])
    },
  })

  const requestOtp = useMutation({
    mutationFn: authApi.requestOtp,
    onSuccess: () => {
      setOtpRequested(true)
      setFormErrors([])
      toast.success('OTP kodu hazirlandi.')
    },
    onError: (error) => {
      const apiError = getApiError(error)
      setFormErrors(apiError.details?.length ? apiError.details : [apiError.message])
    },
  })

  const verifyOtp = useMutation({
    mutationFn: authApi.verifyOtp,
    onSuccess: completeLogin,
    onError: (error) => {
      const apiError = getApiError(error)
      setFormErrors(apiError.details?.length ? apiError.details : [apiError.message])
    },
  })

  const isBusy = staffLogin.isPending || requestOtp.isPending || verifyOtp.isPending

  const handleStaffSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const parsed = staffLoginSchema.safeParse({ email, password })

    if (!parsed.success) {
      setFormErrors(parsed.error.issues.map((issue) => issue.message))
      return
    }

    setFormErrors([])
    staffLogin.mutate(parsed.data)
  }

  const handleOtpRequest = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const parsed = otpRequestSchema.safeParse({ gsmNumber })

    if (!parsed.success) {
      setFormErrors(parsed.error.issues.map((issue) => issue.message))
      return
    }

    requestOtp.mutate(parsed.data)
  }

  const handleOtpVerify = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const parsed = otpVerifySchema.safeParse({ gsmNumber, otpCode })

    if (!parsed.success) {
      setFormErrors(parsed.error.issues.map((issue) => issue.message))
      return
    }

    verifyOtp.mutate(parsed.data)
  }

  return (
    <main className="min-h-screen bg-slate-950 text-white">
      <div className="grid min-h-screen grid-cols-1 lg:grid-cols-[1fr_30rem]">
        <section className="flex items-center px-5 py-8 sm:px-8 lg:px-12">
          <div className="mx-auto w-full max-w-3xl">
            <div className="mb-10 flex items-center gap-3">
              <div className="flex size-11 items-center justify-center rounded-md bg-brand-yellow text-brand-navy">
                <Shield size={24} aria-hidden="true" />
              </div>
              <div>
                <p className="text-lg font-bold text-white">CampaignCell</p>
                <p className="text-sm font-medium text-slate-400">AI kampanya operasyonu</p>
              </div>
            </div>

            <div className="max-w-2xl">
              <p className="text-sm font-bold uppercase text-brand-yellow">Yarisma demosu</p>
              <h1 className="mt-3 text-3xl font-bold leading-tight text-white sm:text-5xl">
                Roller, yetkiler ve mock API tek giris noktasindan baslar.
              </h1>
              <p className="mt-5 max-w-xl text-base leading-7 text-slate-300">
                Backend endpointleri netlestikce bu ekran ayni kalacak; sadece API adaptorleri
                gercek kontrata baglanacak.
              </p>
            </div>

            <div className="mt-8 grid max-w-2xl grid-cols-1 gap-3 sm:grid-cols-3">
              {[
                ['JWT', 'Header hazir'],
                ['Role guard', 'Aktif'],
                ['MSW', 'Mock acik'],
              ].map(([title, value]) => (
                <div className="rounded-md border border-white/10 bg-white/5 p-4" key={title}>
                  <p className="text-xs font-bold uppercase text-slate-400">{title}</p>
                  <p className="mt-2 text-sm font-semibold text-white">{value}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="flex items-center bg-white px-5 py-8 text-slate-950 sm:px-8 lg:px-10">
          <div className="mx-auto w-full max-w-md">
            <div className="mb-6">
              <p className="text-sm font-bold uppercase text-brand-navy">Giris</p>
              <h2 className="mt-2 text-2xl font-bold">Rolune gore devam et</h2>
            </div>

            <div className="mb-5 grid grid-cols-2 rounded-md border border-slate-200 bg-slate-100 p-1">
              <button
                className={`rounded-sm px-3 py-2 text-sm font-bold transition ${
                  mode === 'staff' ? 'bg-white text-brand-navy shadow-sm' : 'text-slate-600'
                }`}
                onClick={() => {
                  setMode('staff')
                  setFormErrors([])
                }}
                type="button"
              >
                <span className="inline-flex items-center gap-2">
                  <LockKeyhole size={16} aria-hidden="true" />
                  Personel
                </span>
              </button>
              <button
                className={`rounded-sm px-3 py-2 text-sm font-bold transition ${
                  mode === 'subscriber' ? 'bg-white text-brand-navy shadow-sm' : 'text-slate-600'
                }`}
                onClick={() => {
                  setMode('subscriber')
                  setFormErrors([])
                }}
                type="button"
              >
                <span className="inline-flex items-center gap-2">
                  <Phone size={16} aria-hidden="true" />
                  Musteri
                </span>
              </button>
            </div>

            {formErrors.length > 0 ? (
              <div className="mb-5 rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <p className="font-bold">Giris tamamlanamadi</p>
                <ul className="mt-2 list-disc space-y-1 pl-5">
                  {formErrors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            {mode === 'staff' ? (
              <form className="space-y-4" onSubmit={handleStaffSubmit}>
                <label className="block">
                  <span className="text-sm font-semibold text-slate-700">E-posta</span>
                  <input
                    className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                    onChange={(event) => setEmail(event.target.value)}
                    type="email"
                    value={email}
                  />
                </label>

                <label className="block">
                  <span className="text-sm font-semibold text-slate-700">Sifre</span>
                  <input
                    className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                    onChange={(event) => setPassword(event.target.value)}
                    type="password"
                    value={password}
                  />
                </label>

                <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
                  <p className="text-xs font-bold uppercase text-slate-500">Mock hesaplar</p>
                  <div className="mt-2 grid gap-2">
                    {demoAccounts.map((account) => (
                      <button
                        className="text-left text-sm font-semibold text-brand-navy hover:text-slate-950"
                        key={account}
                        onClick={() => setEmail(account)}
                        type="button"
                      >
                        {account}
                      </button>
                    ))}
                  </div>
                </div>

                <button
                  className="flex h-11 w-full items-center justify-center gap-2 rounded-md bg-brand-navy px-4 text-sm font-bold text-white transition hover:bg-brand-ink disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isBusy}
                  type="submit"
                >
                  {isBusy ? <Loader2 className="animate-spin" size={18} /> : <ArrowRight size={18} />}
                  Giris yap
                </button>
              </form>
            ) : (
              <form
                className="space-y-4"
                onSubmit={otpRequested ? handleOtpVerify : handleOtpRequest}
              >
                <label className="block">
                  <span className="text-sm font-semibold text-slate-700">GSM numarasi</span>
                  <input
                    className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                    onChange={(event) => setGsmNumber(event.target.value)}
                    type="tel"
                    value={gsmNumber}
                  />
                </label>

                {otpRequested ? (
                  <label className="block">
                    <span className="text-sm font-semibold text-slate-700">OTP kodu</span>
                    <input
                      className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                      inputMode="numeric"
                      onChange={(event) => setOtpCode(event.target.value)}
                      value={otpCode}
                    />
                  </label>
                ) : null}

                <div className="flex items-center gap-2 rounded-md border border-brand-yellow/60 bg-brand-yellow/15 p-3 text-sm font-semibold text-brand-navy">
                  <KeyRound size={17} aria-hidden="true" />
                  OTP ipucu: 1234
                </div>

                <button
                  className="flex h-11 w-full items-center justify-center gap-2 rounded-md bg-brand-navy px-4 text-sm font-bold text-white transition hover:bg-brand-ink disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isBusy}
                  type="submit"
                >
                  {isBusy ? (
                    <Loader2 className="animate-spin" size={18} />
                  ) : (
                    <BadgeCheck size={18} />
                  )}
                  {otpRequested ? 'OTP ile giris yap' : 'OTP iste'}
                </button>
              </form>
            )}
          </div>
        </section>
      </div>
    </main>
  )
}
