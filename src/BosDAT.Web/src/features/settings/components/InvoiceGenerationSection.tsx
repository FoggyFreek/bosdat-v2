import { useState } from 'react'
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

  return (
    <div className="flex items-center justify-between py-3 border-b last:border-b-0">
      <div className="flex items-center gap-3">
        {isSuccess
          ? <CheckCircle2 className="h-4 w-4 text-green-600" />
          : isPartial
            ? <AlertTriangle className="h-4 w-4 text-yellow-600" />
            : <XCircle className="h-4 w-4 text-red-600" />}
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

function RunHistory({ runs, isLoading }: { runs: InvoiceRun[]; isLoading: boolean }) {
  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium">Recent Invoice Runs</h3>
      {runs.length === 0 && !isLoading && (
        <p className="text-sm text-muted-foreground">No invoice generation runs yet.</p>
      )}
      {runs.length > 0 && (
        <div>{runs.map((run) => <RunItem key={run.id} run={run} />)}</div>
      )}
      {isLoading && (
        <div className="flex items-center gap-2 text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span className="text-sm">Loading...</span>
        </div>
      )}
    </div>
  )
}

function GenerateArea({
  onRun,
  isPending,
  lastResult,
}: {
  onRun: (periodType: string, periodStart: string, periodEnd: string) => void
  isPending: boolean
  lastResult: InvoiceRunResult | null
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
        Generate Invoices
      </h3>
      <p className="text-sm text-muted-foreground">
        Generate invoices in bulk for all active enrollments matching the selected period type.
        Enrollments that already have invoices for this period will be skipped.
      </p>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Period Type</Label>
          <Select value={periodType} onValueChange={handlePeriodTypeChange}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Monthly">Monthly</SelectItem>
              <SelectItem value="Quarterly">Quarterly</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label>Period</Label>
          <Select value={selectedPeriod} onValueChange={setSelectedPeriod}>
            <SelectTrigger>
              <SelectValue placeholder="Select period..." />
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
            Invoice run {lastResult.status === 'Success' ? 'completed' : 'failed'}
          </AlertTitle>
          <AlertDescription>
            {lastResult.totalEnrollmentsProcessed} enrollments processed,{' '}
            {lastResult.totalInvoicesGenerated} invoices generated,{' '}
            {lastResult.totalSkipped} skipped
            {lastResult.totalFailed > 0 && `, ${lastResult.totalFailed} failed`}.
            {lastResult.totalAmount > 0 && ` Total: ${formatCurrency(lastResult.totalAmount)}.`}
            {' '}Duration: {formatDuration(lastResult.durationMs)}.
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
        Run Invoice Generation
      </Button>

      <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Run Bulk Invoice Generation?</AlertDialogTitle>
            <AlertDialogDescription>
              This will generate {periodType.toLowerCase()} invoices for all active enrollments
              in the period {selectedPeriodData?.label ?? selectedPeriod}.
              Enrollments that already have invoices for this period will be skipped.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (selectedPeriodData) {
                  onRun(periodType, selectedPeriodData.start, selectedPeriodData.end)
                }
                setShowConfirm(false)
              }}
            >
              Yes, generate invoices
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function ErrorState() {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Receipt className="h-5 w-5" />
          Invoice Generation
        </CardTitle>
        <CardDescription>Bulk invoice generation and run history</CardDescription>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>
            Failed to load invoice generation data. This feature may only be available for Admin users.
          </AlertDescription>
        </Alert>
      </CardContent>
    </Card>
  )
}

export function InvoiceGenerationSection() {
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
    return <ErrorState />
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Receipt className="h-5 w-5" />
          Invoice Generation
        </CardTitle>
        <CardDescription>
          Generate invoices in bulk for all active enrollments. Each run is logged with statistics.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <RunHistory runs={runs} isLoading={isLoadingRuns} />

        <GenerateArea
          onRun={(periodType, periodStart, periodEnd) =>
            runMutation.mutate({ periodType, periodStart, periodEnd })
          }
          isPending={runMutation.isPending}
          lastResult={lastResult}
        />
      </CardContent>
    </Card>
  )
}
