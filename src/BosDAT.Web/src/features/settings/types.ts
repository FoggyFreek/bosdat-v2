import type { ReactNode } from 'react'

export type SettingKey =
  | 'profile'
  | 'preferences'
  | 'instruments'
  | 'course-types'
  | 'rooms'
  | 'holidays'
  | 'system'
  | 'seeding'

export interface NavItem {
  key: SettingKey
  label: string
  icon: ReactNode
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

export interface SystemSetting {
  key: string
  value: string
  type?: string
  description?: string
}
