import { useState } from 'react'
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
import { SettingsDirtyProvider, useSettingsDirty } from '@/context/SettingsDirtyContext'
import {
  SettingsNavigation,
  ProfileSection,
  PreferencesSection,
  SystemSettingsSection,
  InstrumentsSection,
  CourseTypesSection,
  RoomsSection,
  HolidaysSection,
} from '@/components/settings'
import type { SettingKey } from '@/features/settings/types'

function SettingsContent() {
  const [selectedSetting, setSelectedSetting] = useState<SettingKey>('instruments')
  const [pendingNavigation, setPendingNavigation] = useState<SettingKey | null>(null)
  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)
  const { isDirty, setIsDirty } = useSettingsDirty()

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
        return <ProfileSection />
      case 'preferences':
        return <PreferencesSection />
      case 'instruments':
        return <InstrumentsSection />
      case 'course-types':
        return <CourseTypesSection />
      case 'rooms':
        return <RoomsSection />
      case 'holidays':
        return <HolidaysSection />
      case 'system':
        return <SystemSettingsSection />
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
    <SettingsDirtyProvider>
      <SettingsContent />
    </SettingsDirtyProvider>
  )
}
