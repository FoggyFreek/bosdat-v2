import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Clock, CalendarCheck, Loader2, CheckCircle2, XCircle, Play, AlertTriangle,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
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
import { schedulingApi } from '@/features/settings/api'
import type { SchedulingStatus, ScheduleRun, ScheduleRunsResponse, ManualRunResult } from '@/features/settings/types'

function StatusCard({ status, t }: Readonly<{ readonly status: SchedulingStatus; readonly t: TFunction }>) {
  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium flex items-center gap-2">
        <CalendarCheck className="h-4 w-4" />
        {t('settings.scheduling.statusCard.title')}
      </h3>
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">{t('settings.scheduling.statusCard.plannedUntil')}:</span>
          <span className="text-sm font-medium">
            {status.lastScheduledDate ?? t('settings.scheduling.statusCard.noLessons')}
          </span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">{t('settings.scheduling.statusCard.daysAhead')}:</span>
          <span className="text-sm font-medium">{status.daysAhead} {t('common.time.day', { count: status.daysAhead })}</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">{t('settings.scheduling.statusCard.activeCourses')}:</span>
          <span className="text-sm font-medium">{status.activeCourseCount}</span>
        </div>
      </div>
    </div>
  )
}

function RunItem({ run }: { run: ScheduleRun }) {
  const isSuccess = run.status === 'Success'
  return (
    <div className="flex items-center justify-between py-3 border-b last:border-b-0">
      <div className="flex items-center gap-3">
        {isSuccess
          ? <CheckCircle2 className="h-4 w-4 text-green-600" />
          : <XCircle className="h-4 w-4 text-red-600" />}
        <div>
          <div className="text-sm font-medium">
            {run.startDate} - {run.endDate}
          </div>
          <div className="text-xs text-muted-foreground">
            {new Date(run.createdAt).toLocaleString()} &middot; {run.initiatedBy}
          </div>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <Badge variant={isSuccess ? 'default' : 'destructive'}>{run.status}</Badge>
        <span className="text-xs text-muted-foreground">
          {run.totalLessonsCreated} created, {run.totalLessonsSkipped} skipped
        </span>
      </div>
    </div>
  )
}

function RunHistory({
  runs,
  totalCount,
  isLoading,
  onLoadMore,
  t,
}: {
  readonly runs: ScheduleRun[]
  readonly totalCount: number
  readonly isLoading: boolean
  readonly onLoadMore: () => void
  readonly t: TFunction
}) {
  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium">{t('settings.scheduling.runHistory.title')}</h3>
      {runs.length === 0 && !isLoading && (
        <p className="text-sm text-muted-foreground">{t('settings.scheduling.runHistory.noRuns')}</p>
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
      {runs.length < totalCount && !isLoading && (
        <Button variant="outline" size="sm" onClick={onLoadMore}>
          {t('settings.scheduling.runHistory.loadMore')}
        </Button>
      )}
    </div>
  )
}

function AdminArea({
  onRunManual,
  isPending,
  lastResult,
  t,
}: {
  readonly onRunManual: () => void
  readonly isPending: boolean
  readonly lastResult: ManualRunResult | null
  readonly t: TFunction
}) {
  const [showConfirm, setShowConfirm] = useState(false)

  return (
    <div className="rounded-lg border border-yellow-200 bg-yellow-50/30 p-4 space-y-3">
      <h3 className="font-medium flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-yellow-600" />
        {t('settings.scheduling.adminArea.title')}
      </h3>
      <p className="text-sm text-muted-foreground">
        {t('settings.scheduling.adminArea.description')}
      </p>

      {lastResult && (
        <Alert
          variant={lastResult.status === 'Success' ? 'default' : 'destructive'}
          className={lastResult.status === 'Success' ? 'border-green-200 bg-green-50 text-green-800' : ''}
        >
          {lastResult.status === 'Success'
            ? <CheckCircle2 className="h-4 w-4" />
            : <XCircle className="h-4 w-4" />}
          <AlertTitle>
            {t('settings.scheduling.adminArea.manualRun', { status: lastResult.status === 'Success' ? t('common.status.completed') : 'failed' })}
          </AlertTitle>
          <AlertDescription>
            {t('settings.scheduling.adminArea.result', { courses: lastResult.totalCoursesProcessed, created: lastResult.totalLessonsCreated, skipped: lastResult.totalLessonsSkipped })}
          </AlertDescription>
        </Alert>
      )}

      <Button onClick={() => setShowConfirm(true)} disabled={isPending}>
        {isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Play className="h-4 w-4 mr-2" />}
        {t('settings.scheduling.adminArea.runButton')}
      </Button>

      <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('settings.scheduling.adminArea.confirmTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('settings.scheduling.adminArea.confirmDescription')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                onRunManual()
                setShowConfirm(false)
              }}
            >
              {t('settings.scheduling.adminArea.confirmButton')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function ErrorState({ t }: Readonly<{ t: TFunction }>) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Clock className="h-5 w-5" />
          {t('settings.scheduling.title')}
        </CardTitle>
        <CardDescription>{t('settings.scheduling.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>{t('common.states.error')}</AlertTitle>
          <AlertDescription>
            {t('settings.scheduling.errorState')}
          </AlertDescription>
        </Alert>
      </CardContent>
    </Card>
  )
}

export function SchedulingSection() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [allRuns, setAllRuns] = useState<ScheduleRun[]>([])
  const [lastResult, setLastResult] = useState<ManualRunResult | null>(null)

  const { data: status, isLoading: isLoadingStatus, error: statusError } = useQuery<SchedulingStatus>({
    queryKey: ['scheduling-status'],
    queryFn: schedulingApi.getStatus,
  })

  const { data: runsData, isLoading: isLoadingRuns } = useQuery<ScheduleRunsResponse>({
    queryKey: ['scheduling-runs', page],
    queryFn: () => schedulingApi.getRuns(page, 5),
  })

  // Accumulate runs from multiple pages
  const runs = page === 1 ? (runsData?.items ?? []) : allRuns
  const totalCount = runsData?.totalCount ?? 0

  const handleLoadMore = () => {
    const nextPage = page + 1
    if (runsData?.items) {
      setAllRuns((prev) => [...prev, ...(runsData.items ?? [])])
    }
    setPage(nextPage)
  }

  const runMutation = useMutation({
    mutationFn: schedulingApi.runManual,
    onSuccess: (data) => {
      setLastResult(data)
      queryClient.invalidateQueries({ queryKey: ['scheduling-status'] })
      queryClient.invalidateQueries({ queryKey: ['scheduling-runs'] })
      setPage(1)
      setAllRuns([])
    },
    onError: () => {
      setLastResult({
        scheduleRunId: '',
        startDate: '',
        endDate: '',
        totalCoursesProcessed: 0,
        totalLessonsCreated: 0,
        totalLessonsSkipped: 0,
        status: 'Failed',
      })
    },
  })

  if (statusError) {
    return <ErrorState t={t} />
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Clock className="h-5 w-5" />
          {t('settings.scheduling.title')}
        </CardTitle>
        <CardDescription>{t('settings.scheduling.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {isLoadingStatus && (
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span>{t('settings.scheduling.loading')}</span>
          </div>
        )}
        {!isLoadingStatus && status && <StatusCard status={status} t={t} />}

        <RunHistory
          runs={runs}
          totalCount={totalCount}
          isLoading={isLoadingRuns}
          onLoadMore={handleLoadMore}
          t={t}
        />

        <AdminArea
          onRunManual={() => runMutation.mutate()}
          isPending={runMutation.isPending}
          lastResult={lastResult}
          t={t}
        />
      </CardContent>
    </Card>
  )
}
