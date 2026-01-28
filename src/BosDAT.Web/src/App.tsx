import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import { Layout } from './components/Layout'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { StudentsPage } from './pages/StudentsPage'
import { StudentDetailPage } from './pages/StudentDetailPage'
import { StudentFormPage } from './pages/StudentFormPage'
import { TeachersPage } from './pages/TeachersPage'
import { TeacherDetailPage } from './pages/TeacherDetailPage'
import { TeacherFormPage } from './pages/TeacherFormPage'
import { CoursesPage } from './pages/CoursesPage'
import { SchedulePage } from './pages/SchedulePage'
import { SettingsPage } from './pages/SettingsPage'
import { EnrollmentPage } from './pages/EnrollmentPage'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
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
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <Layout>
              <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/students" element={<StudentsPage />} />
                <Route path="/students/new" element={<StudentFormPage />} />
                <Route path="/students/:id" element={<StudentDetailPage />} />
                <Route path="/students/:id/edit" element={<StudentFormPage />} />
                <Route path="/teachers" element={<TeachersPage />} />
                <Route path="/teachers/new" element={<TeacherFormPage />} />
                <Route path="/teachers/:id" element={<TeacherDetailPage />} />
                <Route path="/teachers/:id/edit" element={<TeacherFormPage />} />
                <Route path="/courses" element={<CoursesPage />} />
                <Route path="/schedule" element={<SchedulePage />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="/enrollments/new" element={<EnrollmentPage />} />
              </Routes>
            </Layout>
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}

export default App
