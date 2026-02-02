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
import { seederApi, type SeederStatusResponse, type SeederActionResponse } from '@/services/api'

type SeederAction = 'seed' | 'reset' | 'reseed'

interface ActionConfig {
  title: string
  description: string
  warningTitle: string
  warningDescription: string
  confirmButtonText: string
  icon: React.ReactNode
  variant: 'default' | 'destructive' | 'outline'
}

const actionConfigs: Record<SeederAction, ActionConfig> = {
  seed: {
    title: 'Seed Database',
    description: 'Populate the database with comprehensive test data including teachers, students, courses, lessons, and invoices.',
    warningTitle: 'Seed Database?',
    warningDescription: 'This will add test data to your database. Existing data will NOT be removed, but seeding can only be done once. Are you sure you want to continue?',
    confirmButtonText: 'Yes, seed database',
    icon: <Database className="h-4 w-4" />,
    variant: 'default',
  },
  reset: {
    title: 'Reset Database',
    description: 'Remove all seeded data while preserving the admin user, global settings, instruments, and rooms.',
    warningTitle: 'Reset Database?',
    warningDescription: 'This action will permanently delete all teachers, students, courses, lessons, invoices, and related data. This cannot be undone. The admin user, settings, instruments, and rooms will be preserved.',
    confirmButtonText: 'Yes, reset database',
    icon: <Trash2 className="h-4 w-4" />,
    variant: 'destructive',
  },
  reseed: {
    title: 'Reset & Reseed',
    description: 'Reset the database and immediately repopulate with fresh test data.',
    warningTitle: 'Reset and Reseed Database?',
    warningDescription: 'This action will permanently delete all existing data and replace it with fresh test data. All current teachers, students, courses, lessons, and invoices will be lost. This cannot be undone.',
    confirmButtonText: 'Yes, reset and reseed',
    icon: <RefreshCw className="h-4 w-4" />,
    variant: 'destructive',
  },
}

export function SeedingSection() {
  const queryClient = useQueryClient()
  const [confirmAction, setConfirmAction] = useState<SeederAction | null>(null)
  const [lastResult, setLastResult] = useState<SeederActionResponse | null>(null)

  const { data: status, isLoading: isLoadingStatus, error: statusError } = useQuery<SeederStatusResponse>({
    queryKey: ['seeder-status'],
    queryFn: () => seederApi.getStatus(),
    refetchInterval: false,
  })

  const seedMutation = useMutation({
    mutationFn: () => seederApi.seed(),
    onSuccess: (data) => {
      setLastResult(data)
      queryClient.invalidateQueries({ queryKey: ['seeder-status'] })
    },
    onError: (error: Error) => {
      setLastResult({ success: false, message: error.message, action: 'Seed' })
    },
  })

  const resetMutation = useMutation({
    mutationFn: () => seederApi.reset(),
    onSuccess: (data) => {
      setLastResult(data)
      queryClient.invalidateQueries({ queryKey: ['seeder-status'] })
    },
    onError: (error: Error) => {
      setLastResult({ success: false, message: error.message, action: 'Reset' })
    },
  })

  const reseedMutation = useMutation({
    mutationFn: () => seederApi.reseed(),
    onSuccess: (data) => {
      setLastResult(data)
      queryClient.invalidateQueries({ queryKey: ['seeder-status'] })
    },
    onError: (error: Error) => {
      setLastResult({ success: false, message: error.message, action: 'Reseed' })
    },
  })

  const isAnyMutationPending = seedMutation.isPending || resetMutation.isPending || reseedMutation.isPending

  const handleConfirm = () => {
    if (!confirmAction) return

    setLastResult(null)

    switch (confirmAction) {
      case 'seed':
        seedMutation.mutate()
        break
      case 'reset':
        resetMutation.mutate()
        break
      case 'reseed':
        reseedMutation.mutate()
        break
    }

    setConfirmAction(null)
  }

  const handleActionClick = (action: SeederAction) => {
    setConfirmAction(action)
  }

  if (statusError) {
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
            {isLoadingStatus ? (
              <div className="flex items-center gap-2 text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                <span>Loading status...</span>
              </div>
            ) : status ? (
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Environment:</span>
                  <span className="font-mono text-sm bg-muted px-2 py-1 rounded">{status.environment}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Database Status:</span>
                  <span className={`inline-flex items-center gap-1 text-sm font-medium ${status.isSeeded ? 'text-green-600' : 'text-gray-500'}`}>
                    {status.isSeeded ? (
                      <>
                        <CheckCircle2 className="h-4 w-4" />
                        Seeded
                      </>
                    ) : (
                      <>
                        <Database className="h-4 w-4" />
                        Not Seeded
                      </>
                    )}
                  </span>
                </div>
              </div>
            ) : null}
          </div>

          {/* Last Result */}
          {lastResult && (
            <Alert variant={lastResult.success ? 'default' : 'destructive'} className={lastResult.success ? 'border-green-200 bg-green-50 text-green-800' : ''}>
              {lastResult.success ? <CheckCircle2 className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
              <AlertTitle>{lastResult.action} {lastResult.success ? 'Successful' : 'Failed'}</AlertTitle>
              <AlertDescription>{lastResult.message}</AlertDescription>
            </Alert>
          )}

          {/* Actions */}
          <div className="space-y-4">
            <h3 className="font-medium">Actions</h3>

            {/* Seed Button */}
            <div className="flex items-start justify-between gap-4 p-4 rounded-lg border">
              <div className="flex-1">
                <h4 className="font-medium">{actionConfigs.seed.title}</h4>
                <p className="text-sm text-muted-foreground mt-1">{actionConfigs.seed.description}</p>
              </div>
              <Button
                onClick={() => handleActionClick('seed')}
                disabled={isAnyMutationPending || !status?.canSeed}
                variant={actionConfigs.seed.variant}
              >
                {seedMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  actionConfigs.seed.icon
                )}
                <span className="ml-2">Seed</span>
              </Button>
            </div>

            {/* Reset Button */}
            <div className="flex items-start justify-between gap-4 p-4 rounded-lg border border-red-200 bg-red-50/30">
              <div className="flex-1">
                <h4 className="font-medium text-red-900">{actionConfigs.reset.title}</h4>
                <p className="text-sm text-red-700/80 mt-1">{actionConfigs.reset.description}</p>
              </div>
              <Button
                onClick={() => handleActionClick('reset')}
                disabled={isAnyMutationPending || !status?.canReset}
                variant={actionConfigs.reset.variant}
              >
                {resetMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  actionConfigs.reset.icon
                )}
                <span className="ml-2">Reset</span>
              </Button>
            </div>

            {/* Reseed Button */}
            <div className="flex items-start justify-between gap-4 p-4 rounded-lg border border-red-200 bg-red-50/30">
              <div className="flex-1">
                <h4 className="font-medium text-red-900">{actionConfigs.reseed.title}</h4>
                <p className="text-sm text-red-700/80 mt-1">{actionConfigs.reseed.description}</p>
              </div>
              <Button
                onClick={() => handleActionClick('reseed')}
                disabled={isAnyMutationPending}
                variant={actionConfigs.reseed.variant}
              >
                {reseedMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  actionConfigs.reseed.icon
                )}
                <span className="ml-2">Reseed</span>
              </Button>
            </div>
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
              {confirmAction && actionConfigs[confirmAction].warningTitle}
            </AlertDialogTitle>
            <AlertDialogDescription className="text-left">
              {confirmAction && actionConfigs[confirmAction].warningDescription}
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
            <AlertDialogAction
              onClick={handleConfirm}
              className={confirmAction !== 'seed' ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90' : ''}
            >
              {confirmAction && actionConfigs[confirmAction].confirmButtonText}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
