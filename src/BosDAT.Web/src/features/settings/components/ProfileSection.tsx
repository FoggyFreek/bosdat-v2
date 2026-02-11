import { useTranslation } from 'react-i18next'
import { User } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function ProfileSection() {
  const { t } = useTranslation()

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('settings.sections.profile')}</CardTitle>
        <CardDescription>{t('common.states.comingSoon')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-center py-12 text-muted-foreground">
          <div className="text-center">
            <User className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>{t('common.states.comingSoon')}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
