import { createContext, useContext, useState, type ReactNode } from 'react'

interface FormDirtyContextType {
  isDirty: boolean
  setIsDirty: (dirty: boolean) => void
}

const FormDirtyContext = createContext<FormDirtyContextType | undefined>(undefined)

export function FormDirtyProvider({ children }: { children: ReactNode }) {
  const [isDirty, setIsDirty] = useState(false)

  return (
    <FormDirtyContext.Provider value={{ isDirty, setIsDirty }}>
      {children}
    </FormDirtyContext.Provider>
  )
}

export function useFormDirty() {
  const context = useContext(FormDirtyContext)
  if (context === undefined) {
    throw new Error('useFormDirty must be used within a FormDirtyProvider')
  }
  return context
}
