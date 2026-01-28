import { Suspense, lazy, useState } from 'react'
import { Loader2 } from 'lucide-react'
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
import { FormDirtyProvider, useFormDirty } from '@/context/FormDirtyContext'
import { SettingsNavigation } from '@/features/settings/components'
import type { SettingKey } from '@/features/settings/types'

// Lazy load settings sections from individual files for actual code splitting
const ProfileSection = lazy(() => import('@/features/settings/components/ProfileSection').then(m => ({ default: m.ProfileSection })))
const PreferencesSection = lazy(() => import('@/features/settings/components/PreferencesSection').then(m => ({ default: m.PreferencesSection })))
const SystemSettingsSection = lazy(() => import('@/features/settings/components/SystemSettingsSection').then(m => ({ default: m.SystemSettingsSection })))
const InstrumentsSection = lazy(() => import('@/features/settings/components/InstrumentsSection').then(m => ({ default: m.InstrumentsSection })))
const CourseTypesSection = lazy(() => import('@/features/settings/components/CourseTypesSection').then(m => ({ default: m.CourseTypesSection })))
const RoomsSection = lazy(() => import('@/features/settings/components/RoomsSection').then(m => ({ default: m.RoomsSection })))
const HolidaysSection = lazy(() => import('@/features/settings/components/HolidaysSection').then(m => ({ default: m.HolidaysSection })))

function SettingsLoadingFallback() {
  return (
    <div className="flex items-center justify-center py-12">
      <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
    </div>
  )
}

function SettingsContent() {
  const [selectedSetting, setSelectedSetting] = useState<SettingKey>('profile')
  const [pendingNavigation, setPendingNavigation] = useState<SettingKey | null>(null)
  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)
  const { isDirty, setIsDirty } = useFormDirty()

  const handleNavigation = (key: SettingKey) => {
    if (isDirty && key !== selectedSetting) {
      setPendingNavigation(key)
      setShowUnsavedDialog(true)
    } else {
      setSelectedSetting(key)
    }
  }

  const handleDiscardChanges = () => {
    setIsDirty(false)
    if (pendingNavigation) {
      setSelectedSetting(pendingNavigation)
      setPendingNavigation(null)
    }
    setShowUnsavedDialog(false)
  }

  const handleCancelNavigation = () => {
    setPendingNavigation(null)
    setShowUnsavedDialog(false)
  }

  const renderContent = () => {
    switch (selectedSetting) {
      case 'profile':
        return <Suspense fallback={<SettingsLoadingFallback />}><ProfileSection /></Suspense>
      case 'preferences':
        return <Suspense fallback={<SettingsLoadingFallback />}><PreferencesSection /></Suspense>
      case 'instruments':
        return <Suspense fallback={<SettingsLoadingFallback />}><InstrumentsSection /></Suspense>
      case 'course-types':
        return <Suspense fallback={<SettingsLoadingFallback />}><CourseTypesSection /></Suspense>
      case 'rooms':
        return <Suspense fallback={<SettingsLoadingFallback />}><RoomsSection /></Suspense>
      case 'holidays':
        return <Suspense fallback={<SettingsLoadingFallback />}><HolidaysSection /></Suspense>
      case 'system':
        return <Suspense fallback={<SettingsLoadingFallback />}><SystemSettingsSection /></Suspense>
      default:
        return null
    }
  }

  return (
    <div className="flex h-[calc(100vh-8rem)]">
      <SettingsNavigation
        selectedSetting={selectedSetting}
        onNavigate={handleNavigation}
      />

      <main className="flex-1 p-6 overflow-y-auto">
        {renderContent()}
      </main>

      <AlertDialog open={showUnsavedDialog} onOpenChange={setShowUnsavedDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Unsaved changes</AlertDialogTitle>
            <AlertDialogDescription>
              You have unsaved changes. Do you want to discard them and navigate away?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={handleCancelNavigation}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDiscardChanges}>Discard changes</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

export function SettingsPage() {
  return (
    <FormDirtyProvider>
      <SettingsContent />
    </FormDirtyProvider>
  )
}
