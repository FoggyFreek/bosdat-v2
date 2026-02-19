import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  Users,
  GraduationCap,
  Calendar,
  Settings,
  Menu,
  X,
  LogOut,
  LayoutDashboard,
  Music,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { LanguageSwitcher } from '@/components/ui/language-switcher'
import { GlobalSearch } from '@/components/GlobalSearch'
import { useAuth } from '@/context/AuthContext'
import { useSchoolName } from '@/hooks/useSchoolName'

interface LayoutProps {
  readonly children: React.ReactNode
}

const navigation = [
  { name: 'navigation.dashboard', href: '/', icon: LayoutDashboard },
  { name: 'navigation.students', href: '/students', icon: Users },
  { name: 'navigation.teachers', href: '/teachers', icon: GraduationCap },
  { name: 'navigation.courses', href: '/courses', icon: Music },
  { name: 'navigation.schedule', href: '/schedule', icon: Calendar },
] as const

export function Layout({ children }: LayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const { schoolName } = useSchoolName()
  const { t } = useTranslation()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <div className="flex min-h-screen bg-gray-50">
      {/* Mobile sidebar */}
      <button
        type="button"
        className={cn(
          'fixed inset-0 z-50 bg-gray-900/80 lg:hidden',
          sidebarOpen ? 'block' : 'hidden'
        )}
        onClick={() => setSidebarOpen(false)}
        aria-label={t('common.actions.closeSidebar')}
      />

      <div
        className={cn(
          'fixed inset-y-0 left-0 z-50 w-72 flex flex-col bg-white shadow-xl transition-transform lg:translate-x-0 lg:static lg:shadow-none',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <div className="flex h-16 items-center justify-between px-6 border-b">
          <Link to="/" className="flex items-center space-x-2">
            <Music className="h-8 w-8 text-primary" />
            <span className="text-xl font-bold">BosDAT</span>
          </Link>
          <button className="lg:hidden" onClick={() => setSidebarOpen(false)}>
            <X className="h-6 w-6" />
          </button>
        </div>

        <nav className="flex flex-1 flex-col gap-1 p-4 overflow-y-auto">
          {navigation.map((item) => {
            const isActive = location.pathname === item.href
            return (
              <Link
                key={item.name}
                to={item.href}
                className={cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-primary text-primary-foreground'
                    : 'text-gray-700 hover:bg-gray-100'
                )}
              >
                <item.icon className="h-5 w-5" />
                {t(item.name)}
              </Link>
            )
          })}
        </nav>
      </div>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-40 flex h-16 items-center gap-4 border-b bg-white px-6 lg:px-8">
          <button className="lg:hidden" onClick={() => setSidebarOpen(true)}>
            <Menu className="h-6 w-6" />
          </button>

          {/* Left: School identity */}
          <div className="hidden lg:flex items-center gap-2 shrink-0">
            <Music className="h-5 w-5 text-primary" />
            <span className="text-sm font-medium truncate max-w-[200px]">{schoolName}</span>
          </div>

          {/* Center: Search */}
          <GlobalSearch />

          {/* Right: User actions */}
          <div className="flex items-center gap-1 shrink-0">
            <LanguageSwitcher />
            <Button
              variant="ghost"
              size="icon"
              title={t('auth.logoutButton')}
              onClick={handleLogout}
            >
              <LogOut className="h-5 w-5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              title={t('navigation.settings')}
              onClick={() => navigate('/settings')}
            >
              <Settings className="h-5 w-5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              title={`${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim()}
              onClick={() => navigate('/settings')}
            >
              <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                <span className="text-xs font-medium text-primary">
                  {user?.firstName?.[0]}
                  {user?.lastName?.[0]}
                </span>
              </div>
            </Button>
          </div>
        </header>

        <main className="flex-1 overflow-hidden p-6 lg:p-8">{children}</main>
      </div>
    </div>
  )
}
