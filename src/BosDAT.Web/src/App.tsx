import { Suspense, lazy } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import { Layout } from './components/Layout'
import { LoadingFallback } from './components/LoadingFallback'
import { ErrorBoundary } from './components/ErrorBoundary'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { SetPasswordPage } from './pages/SetPasswordPage'

// Lazy load route components
const StudentsPage = lazy(() => import('./pages/StudentsPage').then(m => ({ default: m.StudentsPage })))
const StudentDetailPage = lazy(() => import('./pages/StudentDetailPage').then(m => ({ default: m.StudentDetailPage })))
const StudentFormPage = lazy(() => import('./pages/StudentFormPage').then(m => ({ default: m.StudentFormPage })))
const TeachersPage = lazy(() => import('./pages/TeachersPage').then(m => ({ default: m.TeachersPage })))
const TeacherDetailPage = lazy(() => import('./pages/TeacherDetailPage').then(m => ({ default: m.TeacherDetailPage })))
const TeacherFormPage = lazy(() => import('./pages/TeacherFormPage').then(m => ({ default: m.TeacherFormPage })))
const CoursesPage = lazy(() => import('./pages/CoursesPage').then(m => ({ default: m.CoursesPage })))
const CourseDetailPage = lazy(() => import('./pages/CourseDetailPage').then(m => ({ default: m.CourseDetailPage })))
const SchedulePage = lazy(() => import('./pages/SchedulePage').then(m => ({ default: m.SchedulePage })))
const SettingsPage = lazy(() => import('./pages/SettingsPage').then(m => ({ default: m.SettingsPage })))
const EnrollmentPage = lazy(() => import('./pages/EnrollmentPage').then(m => ({ default: m.EnrollmentPage })))
const MoveLessonPage = lazy(() => import('./pages/MoveLessonPage').then(m => ({ default: m.MoveLessonPage })))
const AddLessonPage = lazy(() => import('./pages/AddLessonPage').then(m => ({ default: m.AddLessonPage })))
const LessonDetailPage = lazy(() => import('./pages/LessonDetailPage').then(m => ({ default: m.LessonDetailPage })))

function ProtectedRoute({ children }: { readonly children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/set-password" element={<SetPasswordPage />} />
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <ErrorBoundary>
              <Layout>
                <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/students" element={<Suspense fallback={<LoadingFallback />}><StudentsPage /></Suspense>} />
                <Route path="/students/new" element={<Suspense fallback={<LoadingFallback />}><StudentFormPage /></Suspense>} />
                <Route path="/students/:id" element={<Suspense fallback={<LoadingFallback />}><StudentDetailPage /></Suspense>} />
                <Route path="/students/:id/edit" element={<Suspense fallback={<LoadingFallback />}><StudentFormPage /></Suspense>} />
                <Route path="/teachers" element={<Suspense fallback={<LoadingFallback />}><TeachersPage /></Suspense>} />
                <Route path="/teachers/new" element={<Suspense fallback={<LoadingFallback />}><TeacherFormPage /></Suspense>} />
                <Route path="/teachers/:id" element={<Suspense fallback={<LoadingFallback />}><TeacherDetailPage /></Suspense>} />
                <Route path="/teachers/:id/edit" element={<Suspense fallback={<LoadingFallback />}><TeacherFormPage /></Suspense>} />
                <Route path="/courses" element={<Suspense fallback={<LoadingFallback />}><CoursesPage /></Suspense>} />
                <Route path="/courses/:id" element={<Suspense fallback={<LoadingFallback />}><CourseDetailPage /></Suspense>} />
                <Route path="/courses/:id/lessons/:lessonId/move" element={<Suspense fallback={<LoadingFallback />}><MoveLessonPage /></Suspense>} />
                <Route path="/courses/:id/add-lesson" element={<Suspense fallback={<LoadingFallback />}><AddLessonPage /></Suspense>} />
                <Route path="/schedule" element={<Suspense fallback={<LoadingFallback />}><SchedulePage /></Suspense>} />
                <Route path="/settings" element={<Suspense fallback={<LoadingFallback />}><SettingsPage /></Suspense>} />
                <Route path="/enrollments/new" element={<Suspense fallback={<LoadingFallback />}><EnrollmentPage /></Suspense>} />
                <Route path="/lessons/:lessonId" element={<Suspense fallback={<LoadingFallback />}><LessonDetailPage /></Suspense>} />
              </Routes>
              </Layout>
            </ErrorBoundary>
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}

export default App
