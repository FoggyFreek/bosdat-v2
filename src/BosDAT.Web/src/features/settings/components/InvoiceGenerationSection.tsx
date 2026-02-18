import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Receipt, Loader2, CheckCircle2, XCircle, Play, AlertTriangle, Clock,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { invoiceRunApi } from '@/features/settings/api'
import type { InvoiceRun, InvoiceRunsResponse, InvoiceRunResult } from '@/features/settings/types'

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('nl-NL', { style: 'currency', currency: 'EUR' }).format(amount)
}

function formatDuration(ms: number): string {
  if (ms < 1000) return `${ms}ms`
  const seconds = (ms / 1000).toFixed(1)
  return `${seconds}s`
}

function getPeriodLabel(periodType: string): string {
  return periodType === 'Monthly' ? 'Monthly' : 'Quarterly'
}

function getStatusVariant(status: string): 'default' | 'destructive' | 'secondary' {
  if (status === 'Success') return 'default'
  if (status === 'Failed') return 'destructive'
  return 'secondary'
}

function getMonthlyPeriods(): { label: string; start: string; end: string }[] {
  const now = new Date()
  const periods: { label: string; start: string; end: string }[] = []
  for (let offset = -1; offset <= 2; offset++) {
    const date = new Date(now.getFullYear(), now.getMonth() + offset, 1)
    const year = date.getFullYear()
    const month = date.getMonth()
    const lastDay = new Date(year, month + 1, 0).getDate()
    const monthName = date.toLocaleString('en', { month: 'long' })
    const label = `${monthName} ${year}`
    const start = `${year}-${String(month + 1).padStart(2, '0')}-01`
    const end = `${year}-${String(month + 1).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`
    periods.push({ label, start, end })
  }
  return periods
}

function getQuarterlyPeriods(): { label: string; start: string; end: string }[] {
  const now = new Date()
  const currentQuarter = Math.floor(now.getMonth() / 3)
  const periods: { label: string; start: string; end: string }[] = []
  for (let offset = -1; offset <= 1; offset++) {
    const quarterIndex = currentQuarter + offset
    const year = now.getFullYear() + Math.floor((now.getMonth() + offset * 3) / 12)
    const normalizedQuarter = ((quarterIndex % 4) + 4) % 4
    const startMonth = normalizedQuarter * 3
    const endMonth = startMonth + 2
    const lastDay = new Date(year, endMonth + 1, 0).getDate()
    const startMonthName = new Date(year, startMonth, 1).toLocaleString('en', { month: 'short' })
    const endMonthName = new Date(year, endMonth, 1).toLocaleString('en', { month: 'short' })
    const label = `Q${normalizedQuarter + 1} ${year} (${startMonthName}-${endMonthName})`
    const start = `${year}-${String(startMonth + 1).padStart(2, '0')}-01`
    const end = `${year}-${String(endMonth + 1).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`
    periods.push({ label, start, end })
  }
  return periods
}

function RunItem({ run }: { run: InvoiceRun }) {
  const isSuccess = run.status === 'Success'
  const isPartial = run.status === 'PartialSuccess'

  const partialOrFailedIcon = isPartial
    ? <AlertTriangle className="h-4 w-4 text-yellow-600" />
    : <XCircle className="h-4 w-4 text-red-600" />

  return (
    <div className="flex items-center justify-between py-3 border-b last:border-b-0">
      <div className="flex items-center gap-3">
        {isSuccess ? <CheckCircle2 className="h-4 w-4 text-green-600" /> : partialOrFailedIcon}
        <div>
          <div className="text-sm font-medium">
            {getPeriodLabel(run.periodType)}: {run.periodStart} to {run.periodEnd}
          </div>
          <div className="text-xs text-muted-foreground">
            {new Date(run.createdAt).toLocaleString()} &middot; {run.initiatedBy}
          </div>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <Badge variant={getStatusVariant(run.status)}>{run.status}</Badge>
        <div className="text-right">
          <div className="text-xs text-muted-foreground">
            {run.totalInvoicesGenerated} generated, {run.totalSkipped} skipped
            {run.totalFailed > 0 && `, ${run.totalFailed} failed`}
          </div>
          <div className="text-xs text-muted-foreground flex items-center justify-end gap-1">
            <Clock className="h-3 w-3" />
            {formatDuration(run.durationMs)}
            {run.totalAmount > 0 && ` · ${formatCurrency(run.totalAmount)}`}
          </div>
        </div>
      </div>
    </div>
  )
}

function RunHistory({ runs, isLoading, t }: { runs: InvoiceRun[]; isLoading: boolean; t: TFunction }) {
  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium">{t('settings.invoiceGeneration.runHistory.title')}</h3>
      {runs.length === 0 && !isLoading && (
        <p className="text-sm text-muted-foreground">{t('settings.invoiceGeneration.runHistory.noRuns')}</p>
      )}
      {runs.length > 0 && (
        <div>{runs.map((run) => <RunItem key={run.id} run={run} />)}</div>
      )}
      {isLoading && (
        <div className="flex items-center gap-2 text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span className="text-sm">{t('common.states.loading')}</span>
        </div>
      )}
    </div>
  )
}

function GenerateArea({
  onRun,
  isPending,
  lastResult,
  t,
}: {
  onRun: (periodType: string, periodStart: string, periodEnd: string) => void
  isPending: boolean
  lastResult: InvoiceRunResult | null
  t: TFunction
}) {
  const [showConfirm, setShowConfirm] = useState(false)
  const [periodType, setPeriodType] = useState<string>('Monthly')
  const [selectedPeriod, setSelectedPeriod] = useState<string>('')

  const periods = periodType === 'Monthly' ? getMonthlyPeriods() : getQuarterlyPeriods()

  const handlePeriodTypeChange = (value: string) => {
    setPeriodType(value)
    setSelectedPeriod('')
  }

  const selectedPeriodData = periods.find(p => p.start === selectedPeriod)

  return (
    <div className="rounded-lg border border-yellow-200 bg-yellow-50/30 p-4 space-y-4">
      <h3 className="font-medium flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-yellow-600" />
        {t('settings.invoiceGeneration.generateArea.title')}
      </h3>
      <p className="text-sm text-muted-foreground">
        {t('settings.invoiceGeneration.generateArea.description')}
      </p>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>{t('settings.invoiceGeneration.generateArea.periodType')}</Label>
          <Select value={periodType} onValueChange={handlePeriodTypeChange}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Monthly">{t('common.time.months.january').split(' ')[0].length > 5 ? t('enrollments.step2.monthly') : 'Monthly'}</SelectItem>
              <SelectItem value="Quarterly">{t('enrollments.step2.quarterly')}</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label>{t('settings.invoiceGeneration.generateArea.period')}</Label>
          <Select value={selectedPeriod} onValueChange={setSelectedPeriod}>
            <SelectTrigger>
              <SelectValue placeholder={t('common.form.selectPlaceholder')} />
            </SelectTrigger>
            <SelectContent>
              {periods.map((p) => (
                <SelectItem key={p.start} value={p.start}>
                  {p.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {lastResult && (
        <Alert
          variant={lastResult.status === 'Success' ? 'default' : 'destructive'}
          className={lastResult.status === 'Success' ? 'border-green-200 bg-green-50 text-green-800' : ''}
        >
          {lastResult.status === 'Success'
            ? <CheckCircle2 className="h-4 w-4" />
            : <XCircle className="h-4 w-4" />}
          <AlertTitle>
            {t('settings.invoiceGeneration.generateArea.resultTitle', { status: lastResult.status === 'Success' ? t('common.status.completed') : 'failed' })}
          </AlertTitle>
          <AlertDescription>
            {t('settings.invoiceGeneration.generateArea.resultDescription', {
              enrollments: lastResult.totalEnrollmentsProcessed,
              generated: lastResult.totalInvoicesGenerated,
              skipped: lastResult.totalSkipped,
              failed: lastResult.totalFailed,
              amount: formatCurrency(lastResult.totalAmount),
              duration: formatDuration(lastResult.durationMs),
              hasFailed: lastResult.totalFailed > 0,
              hasAmount: lastResult.totalAmount > 0
            })}
          </AlertDescription>
        </Alert>
      )}

      <Button
        onClick={() => setShowConfirm(true)}
        disabled={isPending || !selectedPeriod}
      >
        {isPending
          ? <Loader2 className="h-4 w-4 animate-spin mr-2" />
          : <Play className="h-4 w-4 mr-2" />}
        {t('settings.invoiceGeneration.generateArea.runButton')}
      </Button>

      <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('settings.invoiceGeneration.generateArea.confirmTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('settings.invoiceGeneration.generateArea.confirmDescription', {
                periodType: periodType.toLowerCase(),
                period: selectedPeriodData?.label ?? selectedPeriod
              })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (selectedPeriodData) {
                  onRun(periodType, selectedPeriodData.start, selectedPeriodData.end)
                }
                setShowConfirm(false)
              }}
            >
              {t('settings.invoiceGeneration.generateArea.confirmButton')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function ErrorState({ t }: { t: TFunction }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Receipt className="h-5 w-5" />
          {t('settings.invoiceGeneration.title')}
        </CardTitle>
        <CardDescription>{t('settings.invoiceGeneration.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>{t('common.states.error')}</AlertTitle>
          <AlertDescription>
            {t('settings.invoiceGeneration.errorState')}
          </AlertDescription>
        </Alert>
      </CardContent>
    </Card>
  )
}

export function InvoiceGenerationSection() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [lastResult, setLastResult] = useState<InvoiceRunResult | null>(null)

  const { data: runsData, isLoading: isLoadingRuns, error: runsError } = useQuery<InvoiceRunsResponse>({
    queryKey: ['invoice-runs', 1],
    queryFn: () => invoiceRunApi.getRuns(1, 5),
  })

  const runs = runsData?.items ?? []

  const runMutation = useMutation({
    mutationFn: (params: { periodType: string; periodStart: string; periodEnd: string }) =>
      invoiceRunApi.runBulk({
        periodStart: params.periodStart,
        periodEnd: params.periodEnd,
        periodType: params.periodType,
        applyLedgerCorrections: true,
      }),
    onSuccess: (data) => {
      setLastResult(data)
      queryClient.invalidateQueries({ queryKey: ['invoice-runs'] })
    },
    onError: () => {
      setLastResult({
        invoiceRunId: '',
        periodStart: '',
        periodEnd: '',
        periodType: '',
        totalEnrollmentsProcessed: 0,
        totalInvoicesGenerated: 0,
        totalSkipped: 0,
        totalFailed: 0,
        totalAmount: 0,
        durationMs: 0,
        status: 'Failed',
      })
    },
  })

  if (runsError) {
    return <ErrorState t={t} />
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Receipt className="h-5 w-5" />
          {t('settings.invoiceGeneration.title')}
        </CardTitle>
        <CardDescription>
          {t('settings.invoiceGeneration.description')}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <RunHistory runs={runs} isLoading={isLoadingRuns} t={t} />

        <GenerateArea
          onRun={(periodType, periodStart, periodEnd) =>
            runMutation.mutate({ periodType, periodStart, periodEnd })
          }
          isPending={runMutation.isPending}
          lastResult={lastResult}
          t={t}
        />
      </CardContent>
    </Card>
  )
}
