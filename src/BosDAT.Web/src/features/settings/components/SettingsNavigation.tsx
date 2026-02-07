import {
  User, SlidersHorizontal, Music, BookOpen,
  DoorOpen, CalendarDays, Clock, Settings2, Database
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SettingKey, NavGroup } from '@/features/settings/types'

const navigationGroups: NavGroup[] = [
  {
    label: 'ACCOUNT',
    items: [
      { key: 'profile', label: 'Profile', icon: <User className="h-4 w-4" /> },
      { key: 'preferences', label: 'Preferences', icon: <SlidersHorizontal className="h-4 w-4" /> },
    ],
  },
  {
    label: 'LESSONS',
    items: [
      { key: 'instruments', label: 'Instruments', icon: <Music className="h-4 w-4" /> },
      { key: 'course-types', label: 'Course types', icon: <BookOpen className="h-4 w-4" /> },
    ],
  },
  {
    label: 'SCHEDULING',
    items: [
      { key: 'rooms', label: 'Rooms', icon: <DoorOpen className="h-4 w-4" /> },
      { key: 'holidays', label: 'Holidays', icon: <CalendarDays className="h-4 w-4" /> },
      { key: 'scheduling', label: 'Scheduling', icon: <Clock className="h-4 w-4" /> },
    ],
  },
  {
    label: 'GENERAL',
    items: [
      { key: 'system', label: 'System settings', icon: <Settings2 className="h-4 w-4" /> },
    ],
  },
  {
    label: 'DATA AND STORAGE',
    items: [
      { key: 'seeding', label: 'Seeding', icon: <Database className="h-4 w-4" /> },
    ],
  },
]

interface SettingsNavigationProps {
  selectedSetting: SettingKey
  onNavigate: (key: SettingKey) => void
}

export function SettingsNavigation({ selectedSetting, onNavigate }: SettingsNavigationProps) {
  return (
    <nav className="w-auto min-w-[200px] border-r bg-muted/30 p-4 overflow-y-auto">
      <h1 className="text-xl font-bold mb-6">Settings</h1>
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
