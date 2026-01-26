import {
  User, Settings2, BookOpen, CalendarDays,
  CalendarX, Receipt, PenLine, Wallet
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { StudentSectionKey, StudentNavGroup } from '@/features/students/types'

const navigationGroups: StudentNavGroup[] = [
  {
    label: 'GENERAL',
    items: [
      { key: 'profile', label: 'Profile', icon: <User className="h-4 w-4" /> },
      { key: 'preferences', label: 'Preferences', icon: <Settings2 className="h-4 w-4" /> },
    ],
  },
  {
    label: 'SCHEDULING',
    items: [
      { key: 'enrollments', label: 'Enrollments', icon: <BookOpen className="h-4 w-4" /> },
      { key: 'lessons', label: 'Lessons', icon: <CalendarDays className="h-4 w-4" /> },
      { key: 'absence', label: 'Absence', icon: <CalendarX className="h-4 w-4" /> },
    ],
  },
  {
    label: 'FINANCE',
    items: [
      { key: 'invoices', label: 'Invoices', icon: <Receipt className="h-4 w-4" /> },
      { key: 'corrections', label: 'Corrections', icon: <PenLine className="h-4 w-4" /> },
      { key: 'balance', label: 'Balance', icon: <Wallet className="h-4 w-4" /> },
    ],
  },
]

interface StudentNavigationProps {
  selectedSection: StudentSectionKey
  onNavigate: (key: StudentSectionKey) => void
}

export function StudentNavigation({ selectedSection, onNavigate }: StudentNavigationProps) {
  return (
    <nav className="w-auto min-w-[200px] border-r bg-muted/30 p-4 overflow-y-auto">
      <div className="space-y-6">
        {navigationGroups.map((group) => (
          <div key={group.label}>
            <h2 className="text-xs font-medium text-muted-foreground tracking-wider mb-2">
              {group.label}
            </h2>
            <div className="space-y-1">
              {group.items.map((item) => (
                <button
                  key={item.key}
                  onClick={() => onNavigate(item.key)}
                  className={cn(
                    'w-full flex items-center gap-3 px-3 py-2 text-sm rounded-md transition-colors text-left',
                    selectedSection === item.key
                      ? 'bg-primary text-primary-foreground'
                      : 'hover:bg-muted'
                  )}
                >
                  {item.icon}
                  {item.label}
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>
    </nav>
  )
}
