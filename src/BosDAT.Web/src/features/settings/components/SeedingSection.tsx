import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, Database, RefreshCw, Trash2, Loader2, CheckCircle2, XCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
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
import { seederApi } from '@/features/settings/api'
import type { SeederStatusResponse, SeederActionResponse } from '@/features/settings/types'

type SeederAction = 'seed' | 'reset' | 'reseed'

interface ActionConfig {
  title: string
  description: string
  warningTitle: string
  warningDescription: string
  confirmButtonText: string
  icon: React.ReactNode
  variant: 'default' | 'destructive' | 'outline'
  isDestructive: boolean
}

const ACTION_CONFIGS: Record<SeederAction, ActionConfig> = {
  seed: {
    title: 'Seed Database',
    description: 'Populate the database with comprehensive test data including teachers, students, courses, lessons, and invoices.',
    warningTitle: 'Seed Database?',
    warningDescription: 'This will add test data to your database. Existing data will NOT be removed, but seeding can only be done once. Are you sure you want to continue?',
    confirmButtonText: 'Yes, seed database',
    icon: <Database className="h-4 w-4" />,
    variant: 'default',
    isDestructive: false,
  },
  reset: {
    title: 'Reset Database',
    description: 'Remove all seeded data while preserving the admin user, global settings, instruments, and rooms.',
    warningTitle: 'Reset Database?',
    warningDescription: 'This action will permanently delete all teachers, students, courses, lessons, invoices, and related data. This cannot be undone. The admin user, settings, instruments, and rooms will be preserved.',
    confirmButtonText: 'Yes, reset database',
    icon: <Trash2 className="h-4 w-4" />,
    variant: 'destructive',
    isDestructive: true,
  },
  reseed: {
    title: 'Reset & Reseed',
    description: 'Reset the database and immediately repopulate with fresh test data.',
    warningTitle: 'Reset and Reseed Database?',
    warningDescription: 'This action will permanently delete all existing data and replace it with fresh test data. All current teachers, students, courses, lessons, and invoices will be lost. This cannot be undone.',
    confirmButtonText: 'Yes, reset and reseed',
    icon: <RefreshCw className="h-4 w-4" />,
    variant: 'destructive',
    isDestructive: true,
  },
}

// Sub-components to reduce cognitive complexity
function StatusLoadingState() {
  return (
    <div className="flex items-center gap-2 text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      <span>Loading status...</span>
    </div>
  )
}

function StatusDisplay({ status }: { status: SeederStatusResponse }) {
  const statusColor = status.isSeeded ? 'text-green-600' : 'text-gray-500'
  const StatusIcon = status.isSeeded ? CheckCircle2 : Database
  const statusText = status.isSeeded ? 'Seeded' : 'Not Seeded'

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-sm text-muted-foreground">Environment:</span>
        <span className="font-mono text-sm bg-muted px-2 py-1 rounded">{status.environment}</span>
      </div>
      <div className="flex items-center justify-between">
        <span className="text-sm text-muted-foreground">Database Status:</span>
        <span className={`inline-flex items-center gap-1 text-sm font-medium ${statusColor}`}>
          <StatusIcon className="h-4 w-4" />
          {statusText}
        </span>
      </div>
    </div>
  )
}

function ResultAlert({ result }: { result: SeederActionResponse }) {
  const alertVariant = result.success ? 'default' : 'destructive'
  const alertClassName = result.success ? 'border-green-200 bg-green-50 text-green-800' : ''
  const Icon = result.success ? CheckCircle2 : XCircle
  const statusText = result.success ? 'Successful' : 'Failed'

  return (
    <Alert variant={alertVariant} className={alertClassName}>
      <Icon className="h-4 w-4" />
      <AlertTitle>{result.action} {statusText}</AlertTitle>
      <AlertDescription>{result.message}</AlertDescription>
    </Alert>
  )
}

interface ActionButtonProps {
  action: SeederAction
  config: ActionConfig
  isPending: boolean
  isDisabled: boolean
  onClick: () => void
}

function ActionButton({ action, config, isPending, isDisabled, onClick }: ActionButtonProps) {
  const containerClass = config.isDestructive
    ? 'flex items-start justify-between gap-4 p-4 rounded-lg border border-red-200 bg-red-50/30'
    : 'flex items-start justify-between gap-4 p-4 rounded-lg border'

  const titleClass = config.isDestructive ? 'font-medium text-red-900' : 'font-medium'
  const descClass = config.isDestructive ? 'text-sm text-red-700/80 mt-1' : 'text-sm text-muted-foreground mt-1'

  const buttonContent = isPending
    ? <Loader2 className="h-4 w-4 animate-spin mr-2" />
    : config.icon

  const buttonLabel = action.charAt(0).toUpperCase() + action.slice(1)

  return (
    <div className={containerClass}>
      <div className="flex-1">
        <h4 className={titleClass}>{config.title}</h4>
        <p className={descClass}>{config.description}</p>
      </div>
      <Button
        onClick={onClick}
        disabled={isDisabled}
        variant={config.variant}
      >
        {buttonContent}
        <span className="ml-2">{buttonLabel}</span>
      </Button>
    </div>
  )
}

function ErrorState() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Database Seeding</CardTitle>
        <CardDescription>Manage test data for development</CardDescription>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>
            Failed to load seeder status. This feature may only be available in the Development environment or for Admin users.
          </AlertDescription>
        </Alert>
      </CardContent>
    </Card>
  )
}

function createMutationOptions(
  queryClient: ReturnType<typeof useQueryClient>,
  setLastResult: (result: SeederActionResponse) => void,
  actionName: string
) {
  return {
    onSuccess: (data: SeederActionResponse) => {
      setLastResult(data)
      queryClient.invalidateQueries({ queryKey: ['seeder-status'] })
    },
    onError: (error: Error) => {
      setLastResult({ success: false, message: error.message, action: actionName })
    },
  }
}

export function SeedingSection() {
  const queryClient = useQueryClient()
  const [confirmAction, setConfirmAction] = useState<SeederAction | null>(null)
  const [lastResult, setLastResult] = useState<SeederActionResponse | null>(null)

  const { data: status, isLoading: isLoadingStatus, error: statusError } = useQuery<SeederStatusResponse>({
    queryKey: ['seeder-status'],
    queryFn: seederApi.getStatus,
    refetchInterval: false,
  })

  const seedMutation = useMutation({
    mutationFn: seederApi.seed,
    ...createMutationOptions(queryClient, setLastResult, 'Seed'),
  })

  const resetMutation = useMutation({
    mutationFn: seederApi.reset,
    ...createMutationOptions(queryClient, setLastResult, 'Reset'),
  })

  const reseedMutation = useMutation({
    mutationFn: seederApi.reseed,
    ...createMutationOptions(queryClient, setLastResult, 'Reseed'),
  })

  const mutations = { seed: seedMutation, reset: resetMutation, reseed: reseedMutation }
  const isAnyMutationPending = seedMutation.isPending || resetMutation.isPending || reseedMutation.isPending

  const handleConfirm = () => {
    if (!confirmAction) return

    setLastResult(null)
    mutations[confirmAction].mutate()
    setConfirmAction(null)
  }

  const isActionDisabled = (action: SeederAction): boolean => {
    if (isAnyMutationPending) return true
    if (action === 'seed') return !status?.canSeed
    if (action === 'reset') return !status?.canReset
    return false
  }

  if (statusError) {
    return <ErrorState />
  }

  const currentConfig = confirmAction ? ACTION_CONFIGS[confirmAction] : null
  const confirmButtonClass = confirmAction !== 'seed'
    ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90'
    : ''

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Database Seeding</CardTitle>
          <CardDescription>Manage test data for development environments</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Warning Banner */}
          <Alert className="border-yellow-200 bg-yellow-50 text-yellow-800">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle>Development Only</AlertTitle>
            <AlertDescription>
              These actions are intended for development and testing purposes only.
              They will modify your database and can cause permanent data loss.
            </AlertDescription>
          </Alert>

          {/* Status Section */}
          <div className="rounded-lg border p-4">
            <h3 className="font-medium mb-3">Current Status</h3>
            {isLoadingStatus && <StatusLoadingState />}
            {!isLoadingStatus && status && <StatusDisplay status={status} />}
          </div>

          {/* Last Result */}
          {lastResult && <ResultAlert result={lastResult} />}

          {/* Actions */}
          <div className="space-y-4">
            <h3 className="font-medium">Actions</h3>

            {(['seed', 'reset', 'reseed'] as const).map((action) => (
              <ActionButton
                key={action}
                action={action}
                config={ACTION_CONFIGS[action]}
                isPending={mutations[action].isPending}
                isDisabled={isActionDisabled(action)}
                onClick={() => setConfirmAction(action)}
              />
            ))}
          </div>

          {/* Data Preservation Info */}
          <div className="rounded-lg border p-4 bg-muted/30">
            <h3 className="font-medium mb-2">What is preserved during reset?</h3>
            <ul className="text-sm text-muted-foreground space-y-1 list-disc list-inside">
              <li>Admin user account</li>
              <li>Global settings (VAT rate, registration fee, etc.)</li>
              <li>Instruments</li>
              <li>Rooms</li>
            </ul>
          </div>
        </CardContent>
      </Card>

      {/* Confirmation Dialog */}
      <AlertDialog open={confirmAction !== null} onOpenChange={(open) => !open && setConfirmAction(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-yellow-500" />
              {currentConfig?.warningTitle}
            </AlertDialogTitle>
            <AlertDialogDescription className="text-left">
              {currentConfig?.warningDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <Alert variant="destructive" className="my-2">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle>Warning</AlertTitle>
            <AlertDescription>
              This action cannot be undone. Make sure you understand the consequences before proceeding.
            </AlertDescription>
          </Alert>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirm} className={confirmButtonClass}>
              {currentConfig?.confirmButtonText}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
