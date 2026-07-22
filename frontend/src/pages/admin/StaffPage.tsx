import { Users } from 'lucide-react'
import { PlaceholderPage } from '../PlaceholderPage'

export function StaffPage() {
  return (
    <PlaceholderPage
      eyebrow="Admin"
      title="Personel yonetimi"
      description="Faz 9'da personel olusturma, expertise secimi ve gecici sifre akisi burada tamamlanacak."
      icon={Users}
    />
  )
}
