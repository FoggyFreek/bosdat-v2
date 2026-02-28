import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import {
  User, SlidersHorizontal, Music, BookOpen,
  DoorOpen, CalendarDays, Clock, Settings2, Database, Receipt, Users
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SettingKey, NavGroup } from '@/features/settings/types'
import { useAuth } from '@/context/AuthContext'

const getNavigationGroups = (t: TFunction, isAdmin: boolean): NavGroup[] => {
  const groups: NavGroup[] = [
    {
      label: t('settings.navigation.account'),
      items: [
        { key: 'profile', label: t('settings.sections.profile'), icon: <User className="h-4 w-4" /> },
        { key: 'preferences', label: t('settings.sections.preferences'), icon: <SlidersHorizontal className="h-4 w-4" /> },
      ],
    },
    {
      label: t('settings.navigation.lessons'),
      items: [
        { key: 'instruments', label: t('settings.sections.instruments'), icon: <Music className="h-4 w-4" /> },
        { key: 'course-types', label: t('settings.sections.courseTypes'), icon: <BookOpen className="h-4 w-4" /> },
      ],
    },
    {
      label: t('settings.navigation.scheduling'),
      items: [
        { key: 'rooms', label: t('settings.sections.rooms'), icon: <DoorOpen className="h-4 w-4" /> },
        { key: 'holidays', label: t('settings.sections.holidays'), icon: <CalendarDays className="h-4 w-4" /> },
        { key: 'scheduling', label: t('settings.sections.scheduling'), icon: <Clock className="h-4 w-4" /> },
      ],
    },
    {
      label: t('settings.navigation.finance'),
      items: [
        { key: 'invoice-generation', label: t('settings.sections.invoiceGeneration'), icon: <Receipt className="h-4 w-4" /> },
      ],
    },
    {
      label: t('settings.navigation.general'),
      items: [
        { key: 'system', label: t('settings.sections.system'), icon: <Settings2 className="h-4 w-4" /> },
      ],
    },
    {
      label: t('settings.navigation.dataAndStorage'),
      items: [
        { key: 'seeding', label: t('settings.sections.seeding'), icon: <Database className="h-4 w-4" /> },
      ],
    },
  ]

  if (isAdmin) {
    groups.push({
      label: t('settings.navigation.administration'),
      items: [
        { key: 'manage-users', label: t('settings.sections.manageUsers'), icon: <Users className="h-4 w-4" /> },
      ],
    })
  }

  return groups
}

interface SettingsNavigationProps {
  readonly selectedSetting: SettingKey
  readonly onNavigate: (key: SettingKey) => void
}

export function SettingsNavigation({ selectedSetting, onNavigate }: SettingsNavigationProps) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin') ?? false
  const navigationGroups = getNavigationGroups(t, isAdmin)

  return (
    <nav className="w-auto min-w-[200px] border-r bg-muted/30 p-4 overflow-y-auto">
      <h1 className="text-xl font-bold mb-6">{t('settings.title')}</h1>
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
                    selectedSetting === item.key
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
