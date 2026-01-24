import { createContext, useContext, useState, type ReactNode } from 'react'

interface SettingsDirtyContextType {
  isDirty: boolean
  setIsDirty: (dirty: boolean) => void
}

const SettingsDirtyContext = createContext<SettingsDirtyContextType | undefined>(undefined)

export function SettingsDirtyProvider({ children }: { children: ReactNode }) {
  const [isDirty, setIsDirty] = useState(false)

  return (
    <SettingsDirtyContext.Provider value={{ isDirty, setIsDirty }}>
      {children}
    </SettingsDirtyContext.Provider>
  )
}

export function useSettingsDirty() {
  const context = useContext(SettingsDirtyContext)
  if (context === undefined) {
    throw new Error('useSettingsDirty must be used within a SettingsDirtyProvider')
  }
  return context
}
