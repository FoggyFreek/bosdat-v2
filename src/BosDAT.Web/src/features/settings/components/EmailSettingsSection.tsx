import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2, Save } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { settingsApi } from '@/features/settings/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import { useToast } from '@/hooks/use-toast'

const SUBJECT_KEY = 'email_invoice_subject_template'
const BODY_KEY = 'email_invoice_body_template'

export function EmailSettingsSection() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { setIsDirty } = useFormDirty()
  const { toast } = useToast()
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [initialSubject, setInitialSubject] = useState('')
  const [initialBody, setInitialBody] = useState('')

  const { data: subjectSetting, isLoading: isLoadingSubject } = useQuery({
    queryKey: ['settings', SUBJECT_KEY],
    queryFn: () => settingsApi.getByKey(SUBJECT_KEY),
  })

  const { data: bodySetting, isLoading: isLoadingBody } = useQuery({
    queryKey: ['settings', BODY_KEY],
    queryFn: () => settingsApi.getByKey(BODY_KEY),
  })

  useEffect(() => {
    if (subjectSetting) {
      setSubject(subjectSetting.value)
      setInitialSubject(subjectSetting.value)
    }
  }, [subjectSetting])

  useEffect(() => {
    if (bodySetting) {
      setBody(bodySetting.value)
      setInitialBody(bodySetting.value)
    }
  }, [bodySetting])

  useEffect(() => {
    setIsDirty(subject !== initialSubject || body !== initialBody)
  }, [subject, body, initialSubject, initialBody, setIsDirty])

  const saveMutation = useMutation({
    mutationFn: async () => {
      await Promise.all([
        settingsApi.update(SUBJECT_KEY, subject),
        settingsApi.update(BODY_KEY, body),
      ])
    },
    onSuccess: () => {
      setInitialSubject(subject)
      setInitialBody(body)
      setIsDirty(false)
      queryClient.invalidateQueries({ queryKey: ['settings'] })
      toast({
        title: t('settings.email.saveSuccess'),
        description: t('settings.email.saveSuccessDescription'),
      })
    },
  })

  const isLoading = isLoadingSubject || isLoadingBody
  const isDirty = subject !== initialSubject || body !== initialBody

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold">{t('settings.sections.email')}</h2>
        <p className="text-muted-foreground">{t('settings.email.description')}</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('settings.email.invoiceEmail')}</CardTitle>
          <CardDescription>{t('settings.email.invoiceEmailDescription')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="subject-template">{t('settings.email.subjectTemplate')}</Label>
            <Input
              id="subject-template"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              placeholder="Factuur {{InvoiceNumber}} - {{SchoolName}}"
            />
            <p className="text-xs text-muted-foreground">
              {t('settings.email.subjectPlaceholders')}
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="body-template">{t('settings.email.bodyTemplate')}</Label>
            <Textarea
              id="body-template"
              value={body}
              onChange={(e) => setBody(e.target.value)}
              className="min-h-[400px] font-mono text-sm"
            />
            <p className="text-xs text-muted-foreground">
              {t('settings.email.bodyPlaceholders')}
            </p>
          </div>

          <div className="flex justify-end">
            <Button
              onClick={() => saveMutation.mutate()}
              disabled={!isDirty || saveMutation.isPending}
            >
              {saveMutation.isPending ? (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Save className="h-4 w-4 mr-2" />
              )}
              {t('common.actions.save')}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
