import { useMutation } from '@tanstack/react-query'
import {
  ArrowRight,
  BadgeCheck,
  Loader2,
  LockKeyhole,
  Phone,
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
  email: z.string().email('Geçerli bir e-posta girin.'),
  password: z.string().min(1, 'Şifre zorunludur.'),
})

const otpRequestSchema = z.object({
  gsmNumber: z
    .string()
    .min(10, 'GSM numarası en az 10 haneli olmalı.')
    .regex(/^[0-9+ ]+$/, 'GSM numarası sadece rakam, boşluk veya + içerebilir.'),
})

const otpVerifySchema = otpRequestSchema.extend({
  otpCode: z.string().length(4, 'OTP kodu 4 haneli olmalı.'),
})

export function LoginPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((state) => state.setSession)
  const [mode, setMode] = useState<LoginMode>('staff')
  // Not: bilerek bos birakiliyor - onceden buraya mock donemine ait sahte demo
  // bilgileri (personel@campaigncell.local / Password1) hardcode edilmisti; gercek
  // backend'e bagli production build'de bu deger gecersiz oldugu icin kafa karistiriyordu.
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [gsmNumber, setGsmNumber] = useState('')
  const [otpCode, setOtpCode] = useState('')
  const [otpRequested, setOtpRequested] = useState(false)
  const [formErrors, setFormErrors] = useState<string[]>([])

  const completeLogin = (session: Awaited<ReturnType<typeof authApi.login>>) => {
    setSession(session)
    toast.success(`${session.user.firstName} ${session.user.lastName} giriş yaptı.`)
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
      toast.success('OTP kodu hazırlandı.')
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
    <main className="min-h-screen overflow-hidden bg-[#294b98] text-white">
      <div className="mx-auto flex min-h-screen w-full max-w-7xl flex-col px-5 py-6 sm:px-8 lg:px-10">
        <header className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <img className="h-10 w-auto object-contain sm:h-12" src="/turkcell-logo.jpeg" alt="CampaignCell" />
            <div>
              <p className="text-lg font-bold leading-5 text-white">CampaignCell</p>
              <p className="text-sm font-medium text-white/70">AI kampanya operasyonu</p>
            </div>
          </div>
        </header>

        <div className="grid flex-1 items-center gap-10 py-10 lg:grid-cols-[1fr_31rem] lg:gap-20">
          <section className="max-w-2xl">
            <p className="text-sm font-bold uppercase text-brand-yellow">CampaignCell</p>
            <h1 className="mt-4 text-4xl font-bold leading-tight text-white sm:text-5xl">
              AI destekli kampanya yönetimi için tek giriş platformu.
            </h1>
            <p className="mt-5 max-w-xl text-base leading-7 text-white/78 sm:text-lg">
              AI destekli kampanya yönetimi ve müşteri operasyonları için güvenli giriş platformu.
            </p>
          </section>

          <section className="rounded-md border border-white/14 bg-white/9 p-5 text-white shadow-2xl shadow-slate-950/20 backdrop-blur sm:p-8">
            <div className="mb-8">
              <p className="text-sm font-bold uppercase text-brand-yellow">Giriş</p>
              <h2 className="mt-2 text-2xl font-bold">Hesabınıza Giriş Yapın</h2>
              <p className="mt-2 text-sm font-medium text-white/70">Devam etmek için giriş yönteminizi seçin.</p>
            </div>

            <div className="mb-6 grid grid-cols-2 rounded-md border border-white/18 bg-white/10 p-1">
              <button
                className={`rounded-sm px-3 py-3 text-sm font-bold transition ${
                  mode === 'staff'
                    ? 'bg-white text-brand-navy shadow-sm'
                    : 'text-white/72 hover:bg-white/8 hover:text-white'
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
                className={`rounded-sm px-3 py-3 text-sm font-bold transition ${
                  mode === 'subscriber'
                    ? 'bg-white text-brand-navy shadow-sm'
                    : 'text-white/72 hover:bg-white/8 hover:text-white'
                }`}
                onClick={() => {
                  setMode('subscriber')
                  setFormErrors([])
                }}
                type="button"
              >
                <span className="inline-flex items-center gap-2">
                  <Phone size={16} aria-hidden="true" />
                  Müşteri
                </span>
              </button>
            </div>

            {formErrors.length > 0 ? (
              <div className="mb-5 rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <p className="font-bold">Giriş tamamlanamadı</p>
                <ul className="mt-2 list-disc space-y-1 pl-5">
                  {formErrors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            {mode === 'staff' ? (
              <form className="space-y-5" onSubmit={handleStaffSubmit}>
                <label className="block">
                  <span className="text-sm font-semibold text-white/88">E-posta</span>
                  <input
                    className="mt-2 h-12 w-full rounded-md border border-white/18 bg-white px-4 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-brand-yellow focus:ring-4 focus:ring-brand-yellow/20"
                    onChange={(event) => setEmail(event.target.value)}
                    type="email"
                    value={email}
                  />
                </label>

                <label className="block">
                  <span className="text-sm font-semibold text-white/88">Şifre</span>
                  <input
                    className="mt-2 h-12 w-full rounded-md border border-white/18 bg-white px-4 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-brand-yellow focus:ring-4 focus:ring-brand-yellow/20"
                    onChange={(event) => setPassword(event.target.value)}
                    type="password"
                    value={password}
                  />
                </label>

                <button
                  className="flex h-12 w-full items-center justify-center gap-2 rounded-md bg-brand-yellow px-4 text-sm font-bold text-brand-navy transition hover:bg-yellow-300 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isBusy}
                  type="submit"
                >
                  {isBusy ? <Loader2 className="animate-spin" size={18} /> : <ArrowRight size={18} />}
                  Giriş Yap
                </button>
              </form>
            ) : (
              <form
                className="space-y-5"
                onSubmit={otpRequested ? handleOtpVerify : handleOtpRequest}
              >
                <label className="block">
                  <span className="text-sm font-semibold text-white/88">GSM numarası</span>
                  <input
                    className="mt-2 h-12 w-full rounded-md border border-white/18 bg-white px-4 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-brand-yellow focus:ring-4 focus:ring-brand-yellow/20"
                    onChange={(event) => setGsmNumber(event.target.value)}
                    type="tel"
                    value={gsmNumber}
                  />
                </label>

                {otpRequested ? (
                  <label className="block">
                    <span className="text-sm font-semibold text-white/88">Doğrulama kodu</span>
                    <input
                      className="mt-2 h-12 w-full rounded-md border border-white/18 bg-white px-4 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-brand-yellow focus:ring-4 focus:ring-brand-yellow/20"
                      inputMode="numeric"
                      onChange={(event) => setOtpCode(event.target.value)}
                      value={otpCode}
                    />
                  </label>
                ) : null}

                <button
                  className="flex h-12 w-full items-center justify-center gap-2 rounded-md bg-brand-yellow px-4 text-sm font-bold text-brand-navy transition hover:bg-yellow-300 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isBusy}
                  type="submit"
                >
                  {isBusy ? (
                    <Loader2 className="animate-spin" size={18} />
                  ) : (
                    <BadgeCheck size={18} />
                  )}
                  {otpRequested ? 'Giriş Yap' : 'Kod Gönder'}
                </button>
              </form>
            )}
          </section>
        </div>
      </div>
    </main>
  )
}
