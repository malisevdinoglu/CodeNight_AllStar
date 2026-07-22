import { Copy, UserPlus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import toast from 'react-hot-toast'
import { z } from 'zod'
import { getApiError } from '../../api/errors'
import type { Segment, UserRole } from '../../api/types'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../components/ui'
import { useCreateStaff } from '../../hooks/useAdmin'

const staffSchema = z.object({
  firstName: z.string().min(2, 'Ad en az 2 karakter olmalı.'),
  lastName: z.string().min(2, 'Soyad en az 2 karakter olmalı.'),
  email: z.string().email('Geçerli bir e-posta girin.'),
  password: z.string().min(8, 'Geçici şifre en az 8 karakter olmalı.'),
  role: z.enum(['PERSONEL', 'SUPERVIZOR', 'ADMIN']),
  expertiseAreas: z.array(z.enum(['YUKSEK_DEGER', 'RISKLI_KAYIP', 'YENI_ABONE', 'PASIF', 'BELIRSIZ'])),
  region: z.string().min(2, 'Bölge seçimi zorunludur.'),
})

const segments: Segment[] = ['RISKLI_KAYIP', 'YUKSEK_DEGER', 'YENI_ABONE', 'PASIF', 'BELIRSIZ']
const regions = ['MARMARA', 'IC_ANADOLU', 'EGE', 'AKDENIZ', 'KARADENIZ', 'DOGU_ANADOLU']
const roles: UserRole[] = ['PERSONEL', 'SUPERVIZOR', 'ADMIN']

const segmentLabels: Record<Segment, string> = {
  RISKLI_KAYIP: 'Riskli kayıp',
  YUKSEK_DEGER: 'Yüksek değer',
  YENI_ABONE: 'Yeni abone',
  PASIF: 'Pasif',
  BELIRSIZ: 'Belirsiz',
}

const regionLabels: Record<string, string> = {
  MARMARA: 'Marmara',
  IC_ANADOLU: 'İç Anadolu',
  EGE: 'Ege',
  AKDENIZ: 'Akdeniz',
  KARADENIZ: 'Karadeniz',
  DOGU_ANADOLU: 'Doğu Anadolu',
}

const roleLabels: Record<UserRole, string> = {
  PERSONEL: 'Personel',
  SUPERVIZOR: 'Süpervizör',
  ADMIN: 'Admin',
  MUSTERI: 'Müşteri',
}

export function StaffPage() {
  const createStaff = useCreateStaff()
  const [firstName, setFirstName] = useState('Yeni')
  const [lastName, setLastName] = useState('Uzman')
  const [email, setEmail] = useState('uzman@campaigncell.local')
  const [password, setPassword] = useState('TempPass1')
  const [role, setRole] = useState<UserRole>('PERSONEL')
  const [region, setRegion] = useState('MARMARA')
  const [expertiseAreas, setExpertiseAreas] = useState<Segment[]>(['RISKLI_KAYIP'])
  const [errors, setErrors] = useState<string[]>([])

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const parsed = staffSchema.safeParse({
      firstName,
      lastName,
      email,
      password,
      role,
      expertiseAreas,
      region,
    })

    if (!parsed.success) {
      setErrors(parsed.error.issues.map((issue) => issue.message))
      return
    }

    setErrors([])
    createStaff.mutate(parsed.data, {
      onSuccess: () => toast.success('Personel oluşturuldu.'),
      onError: (error) => toast.error(getApiError(error).message),
    })
  }

  const toggleExpertise = (segment: Segment) => {
    setExpertiseAreas((current) =>
      current.includes(segment)
        ? current.filter((item) => item !== segment)
        : [...current, segment],
    )
  }

  return (
    <section className="grid gap-6 xl:grid-cols-[1fr_24rem]">
      <Card>
        <CardHeader>
          <CardTitle>Personel Oluştur</CardTitle>
          <CardDescription>
            Admin yetkisiyle personel, süpervizör veya admin hesabı tanımlanır.
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
            <div className="grid gap-5 md:grid-cols-2">
              <TextField label="Ad" onChange={setFirstName} value={firstName} />
              <TextField label="Soyad" onChange={setLastName} value={lastName} />
            </div>

            <TextField label="E-posta" onChange={setEmail} type="email" value={email} />

            <div className="grid gap-5 md:grid-cols-3">
              <TextField label="Geçici şifre" onChange={setPassword} value={password} />
              <SelectField
                label="Rol"
                getLabel={(value) => roleLabels[value as UserRole]}
                onChange={(value) => setRole(value as UserRole)}
                options={roles}
                value={role}
              />
              <SelectField
                getLabel={(value) => regionLabels[value]}
                label="Bölge"
                onChange={setRegion}
                options={regions}
                value={region}
              />
            </div>

            <fieldset>
              <legend className="text-sm font-semibold text-slate-700">Uzmanlık alanları</legend>
              <div className="mt-3 flex flex-wrap gap-2">
                {segments.map((segment) => (
                  <button
                    className={`rounded-md border px-3 py-2 text-sm font-bold transition ${
                      expertiseAreas.includes(segment)
                        ? 'border-brand-yellow bg-brand-yellow/20 text-brand-navy'
                        : 'border-slate-200 bg-white text-slate-700 hover:border-brand-navy/40'
                    }`}
                    key={segment}
                    onClick={() => toggleExpertise(segment)}
                    type="button"
                  >
                    {segmentLabels[segment]}
                  </button>
                ))}
              </div>
            </fieldset>

            <Button
              isLoading={createStaff.isPending}
              leftIcon={<UserPlus size={18} aria-hidden="true" />}
              type="submit"
            >
              Personel Oluştur
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Sonuç</CardTitle>
          <CardDescription>Personel oluşturma işleminin özeti.</CardDescription>
        </CardHeader>
        <CardContent>
          {createStaff.data ? (
            <div className="space-y-4">
              <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
                <p className="text-xs font-bold uppercase text-slate-500">Oluşturulan kullanıcı</p>
                <p className="mt-2 text-lg font-bold text-slate-950">
                  {createStaff.data.user.firstName} {createStaff.data.user.lastName}
                </p>
                <Badge className="mt-2" tone="brand">
                  {createStaff.data.user.role}
                </Badge>
              </div>
              <div className="rounded-md border border-brand-yellow/60 bg-brand-yellow/15 p-4">
                <p className="text-xs font-bold uppercase text-brand-navy">Geçici şifre</p>
                <div className="mt-2 flex items-center justify-between gap-3">
                  <code className="rounded-sm bg-white px-2 py-1 text-sm font-bold text-slate-950">
                    {createStaff.data.temporaryPassword}
                  </code>
                  <Button
                    leftIcon={<Copy size={16} aria-hidden="true" />}
                    onClick={() =>
                      navigator.clipboard
                        .writeText(createStaff.data.temporaryPassword)
                        .then(() => toast.success('Geçici şifre kopyalandı.'))
                        .catch(() => toast.error('Kopyalama tarayıcı tarafından engellendi.'))
                    }
                    size="sm"
                    variant="secondary"
                  >
                    Kopyala
                  </Button>
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm leading-6 text-slate-600">
              Personel oluşturulduğunda geçici şifre ve kullanıcı özeti burada görünür.
            </p>
          )}
        </CardContent>
      </Card>
    </section>
  )
}

function TextField({
  label,
  onChange,
  type = 'text',
  value,
}: {
  label: string
  onChange: (value: string) => void
  type?: string
  value: string
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-slate-700">{label}</span>
      <input
        className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
        onChange={(event) => onChange(event.target.value)}
        type={type}
        value={value}
      />
    </label>
  )
}

function SelectField({
  getLabel,
  label,
  onChange,
  options,
  value,
}: {
  getLabel?: (value: string) => string
  label: string
  onChange: (value: string) => void
  options: string[]
  value: string
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-slate-700">{label}</span>
      <select
        className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
        onChange={(event) => onChange(event.target.value)}
        value={value}
      >
        {options.map((option) => (
          <option key={option} value={option}>
            {getLabel ? getLabel(option) : option}
          </option>
        ))}
      </select>
    </label>
  )
}
