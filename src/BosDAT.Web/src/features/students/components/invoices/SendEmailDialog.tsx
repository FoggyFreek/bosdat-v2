import { useTranslation } from 'react-i18next'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Mail, Paperclip, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { invoicesApi } from '@/features/students/api'
import { useToast } from '@/hooks/use-toast'

interface SendEmailDialogProps {
  readonly open: boolean
  readonly onOpenChange: (open: boolean) => void
  readonly invoiceId: string
  readonly invoiceNumber: string
  readonly studentId: string
}

export function SendEmailDialog({
  open,
  onOpenChange,
  invoiceId,
  invoiceNumber,
  studentId,
}: SendEmailDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const { data: preview, isLoading: isLoadingPreview } = useQuery({
    queryKey: ['invoice-email-preview', invoiceId],
    queryFn: () => invoicesApi.previewEmail(invoiceId),
    enabled: open && !!invoiceId,
  })

  const sendMutation = useMutation({
    mutationFn: () => invoicesApi.sendEmail(invoiceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] })
      toast({
        title: t('students.invoices.sendEmail.successTitle'),
        description: t('students.invoices.sendEmail.successDescription'),
      })
      onOpenChange(false)
    },
    onError: () => {
      toast({
        title: t('students.invoices.sendEmail.errorTitle'),
        description: t('students.invoices.sendEmail.errorDescription'),
        variant: 'destructive',
      })
    },
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            {t('students.invoices.sendEmail.title')}
          </DialogTitle>
          <DialogDescription>
            {t('students.invoices.sendEmail.description')}
          </DialogDescription>
        </DialogHeader>

        {isLoadingPreview ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : preview ? (
          <div className="space-y-4">
            <div className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
              <span className="font-medium text-muted-foreground">
                {t('students.invoices.sendEmail.to')}
              </span>
              <span>{preview.toEmail}</span>
              <span className="font-medium text-muted-foreground">
                {t('students.invoices.sendEmail.subject')}
              </span>
              <span>{preview.subject}</span>
              <span className="font-medium text-muted-foreground">
                <Paperclip className="h-4 w-4 inline mr-1" />
                {t('students.invoices.sendEmail.attachment')}
              </span>
              <span>{invoiceNumber}.pdf</span>
            </div>

            <div className="border rounded-md overflow-hidden">
              <iframe
                srcDoc={preview.htmlBody}
                title={t('students.invoices.sendEmail.preview')}
                className="w-full h-[400px] border-0"
                sandbox=""
              />
            </div>
          </div>
        ) : null}

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
          >
            {t('common.actions.cancel')}
          </Button>
          <Button
            onClick={() => sendMutation.mutate()}
            disabled={sendMutation.isPending || isLoadingPreview || !preview}
          >
            {sendMutation.isPending ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <Mail className="h-4 w-4 mr-2" />
            )}
            {sendMutation.isPending
              ? t('students.invoices.sendEmail.sending')
              : t('students.invoices.sendEmail.send')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
