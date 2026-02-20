import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
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
import {
  StudentNavigation,
  ProfileSection,
  PreferencesSection,
  EnrollmentsSection,
  LessonsSection,
  AbsenceSection,
  InvoicesSection,
  TransactionsSection,
} from '@/features/students/components'
import { studentsApi } from '@/features/students/api'
import type { Student, StudentSectionKey } from '@/features/students/types'

function StudentDetailContent() {
  const { id } = useParams<{ id: string }>()
  const [selectedSection, setSelectedSection] = useState<StudentSectionKey>('profile')
  const [pendingNavigation, setPendingNavigation] = useState<StudentSectionKey | null>(null)
  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)
  const { isDirty, setIsDirty } = useFormDirty()

  const { data: student, isLoading } = useQuery<Student>({
    queryKey: ['student', id],
    queryFn: () => studentsApi.getById(id!),
    enabled: !!id,
  })

  const handleNavigation = (key: StudentSectionKey) => {
    if (isDirty && key !== selectedSection) {
      setPendingNavigation(key)
      setShowUnsavedDialog(true)
    } else {
      setSelectedSection(key)
    }
  }

  const handleDiscardChanges = () => {
    setIsDirty(false)
    if (pendingNavigation) {
      setSelectedSection(pendingNavigation)
      setPendingNavigation(null)
    }
    setShowUnsavedDialog(false)
  }

  const handleCancelNavigation = () => {
    setPendingNavigation(null)
    setShowUnsavedDialog(false)
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800'
      case 'Trial':
        return 'bg-yellow-100 text-yellow-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const renderContent = () => {
    if (!id) return null

    switch (selectedSection) {
      case 'profile':
        return <ProfileSection studentId={id} />
      case 'preferences':
        return <PreferencesSection />
      case 'enrollments':
        return <EnrollmentsSection studentId={id} />
      case 'lessons':
        return <LessonsSection studentId={id} />
      case 'absence':
        return <AbsenceSection studentId={id} />
      case 'invoices':
        return <InvoicesSection studentId={id} />
      case 'transactions':
        return <TransactionsSection studentId={id} />
      default:
        return null
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!student) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Student not found</p>
        <Button asChild className="mt-4">
          <Link to="/students">Back to Students</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-[calc(100vh-4rem)]">
      {/* Header */}
      <div className="flex items-center gap-4 px-6 py-4 border-b">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/students">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{student.fullName}</h1>
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getStatusColor(
              student.status
            )}`}
          >
            {student.status}
          </span>
        </div>
        <Button asChild>
          <Link to={`/students/${id}/edit`}>Edit Student</Link>
        </Button>
      </div>

      {/* Two-column layout */}
      <div className="flex flex-1 overflow-hidden">
        <StudentNavigation
          selectedSection={selectedSection}
          onNavigate={handleNavigation}
        />

        <main className="flex-1 p-6 overflow-y-auto">
          {renderContent()}
        </main>
      </div>

      {/* Unsaved changes dialog */}
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

export function StudentDetailPage() {
  return (
    <FormDirtyProvider>
      <StudentDetailContent />
    </FormDirtyProvider>
  )
}
