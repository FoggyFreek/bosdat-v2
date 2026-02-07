import { useState } from 'react'
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

function StatusCard({ status }: { status: SchedulingStatus }) {
  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium flex items-center gap-2">
        <CalendarCheck className="h-4 w-4" />
        Scheduling Status
      </h3>
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">Planned until:</span>
          <span className="text-sm font-medium">
            {status.lastScheduledDate ?? 'No lessons scheduled'}
          </span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">Days ahead:</span>
          <span className="text-sm font-medium">{status.daysAhead} days</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">Active courses:</span>
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
}: {
  runs: ScheduleRun[]
  totalCount: number
  isLoading: boolean
  onLoadMore: () => void
}) {
  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium">Schedule Run History</h3>
      {runs.length === 0 && !isLoading && (
        <p className="text-sm text-muted-foreground">No scheduling runs yet.</p>
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
      {runs.length < totalCount && !isLoading && (
        <Button variant="outline" size="sm" onClick={onLoadMore}>
          Load more
        </Button>
      )}
    </div>
  )
}

function AdminArea({
  onRunManual,
  isPending,
  lastResult,
}: {
  onRunManual: () => void
  isPending: boolean
  lastResult: ManualRunResult | null
}) {
  const [showConfirm, setShowConfirm] = useState(false)

  return (
    <div className="rounded-lg border border-yellow-200 bg-yellow-50/30 p-4 space-y-3">
      <h3 className="font-medium flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-yellow-600" />
        Administrator Area
      </h3>
      <p className="text-sm text-muted-foreground">
        Trigger a manual bulk lesson generation for the next 90 days across all active courses.
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
            Manual run {lastResult.status === 'Success' ? 'completed' : 'failed'}
          </AlertTitle>
          <AlertDescription>
            {lastResult.totalCoursesProcessed} courses processed,{' '}
            {lastResult.totalLessonsCreated} lessons created,{' '}
            {lastResult.totalLessonsSkipped} skipped.
          </AlertDescription>
        </Alert>
      )}

      <Button onClick={() => setShowConfirm(true)} disabled={isPending}>
        {isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Play className="h-4 w-4 mr-2" />}
        Run Manual Generation
      </Button>

      <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Run Manual Lesson Generation?</AlertDialogTitle>
            <AlertDialogDescription>
              This will generate lessons for the next 90 days for all active courses.
              Existing lessons will not be duplicated.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                onRunManual()
                setShowConfirm(false)
              }}
            >
              Yes, run generation
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
          <Clock className="h-5 w-5" />
          Scheduling
        </CardTitle>
        <CardDescription>Lesson scheduling status and management</CardDescription>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>
            Failed to load scheduling status. This feature may only be available for Admin users.
          </AlertDescription>
        </Alert>
      </CardContent>
    </Card>
  )
}

export function SchedulingSection() {
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
    return <ErrorState />
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Clock className="h-5 w-5" />
          Scheduling
        </CardTitle>
        <CardDescription>Lesson scheduling status and management</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {isLoadingStatus && (
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span>Loading status...</span>
          </div>
        )}
        {!isLoadingStatus && status && <StatusCard status={status} />}

        <RunHistory
          runs={runs}
          totalCount={totalCount}
          isLoading={isLoadingRuns}
          onLoadMore={handleLoadMore}
        />

        <AdminArea
          onRunManual={() => runMutation.mutate()}
          isPending={runMutation.isPending}
          lastResult={lastResult}
        />
      </CardContent>
    </Card>
  )
}
