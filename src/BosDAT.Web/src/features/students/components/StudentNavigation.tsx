import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import {
  User, Settings2, BookOpen, CalendarDays,
  CalendarX, Receipt, PenLine, BookOpenCheck
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { StudentSectionKey, StudentNavGroup } from '@/features/students/types'

const getNavigationGroups = (t: TFunction): StudentNavGroup[] => [
  {
    label: t('students.navigation.general'),
    items: [
      { key: 'profile', label: t('students.sections.profile'), icon: <User className="h-4 w-4" /> },
      { key: 'preferences', label: t('students.sections.preferences'), icon: <Settings2 className="h-4 w-4" /> },
    ],
  },
  {
    label: t('students.navigation.scheduling'),
    items: [
      { key: 'enrollments', label: t('students.sections.enrollments'), icon: <BookOpen className="h-4 w-4" /> },
      { key: 'lessons', label: t('students.sections.lessons'), icon: <CalendarDays className="h-4 w-4" /> },
      { key: 'absence', label: t('students.sections.absences'), icon: <CalendarX className="h-4 w-4" /> },
    ],
  },
  {
    label: t('students.navigation.finance'),
    items: [
      { key: 'invoices', label: t('students.sections.invoices'), icon: <Receipt className="h-4 w-4" /> },
      { key: 'corrections', label: t('students.sections.corrections'), icon: <PenLine className="h-4 w-4" /> },
      { key: 'balance', label: t('students.ledger.title'), icon: <BookOpenCheck className="h-4 w-4" /> },
    ],
  },
]

interface StudentNavigationProps {
  selectedSection: StudentSectionKey
  onNavigate: (key: StudentSectionKey) => void
}

export function StudentNavigation({ selectedSection, onNavigate }: StudentNavigationProps) {
  const { t } = useTranslation()
  const navigationGroups = getNavigationGroups(t)

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
