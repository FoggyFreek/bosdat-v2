import { createContext, useContext, useState, useMemo, type ReactNode } from 'react'

interface FormDirtyContextType {
  isDirty: boolean
  setIsDirty: (dirty: boolean) => void
}

const FormDirtyContext = createContext<FormDirtyContextType | undefined>(undefined)

export function FormDirtyProvider({ children }: { readonly children: ReactNode }) {
  const [isDirty, setIsDirty] = useState(false)

  const value = useMemo(() => ({ isDirty, setIsDirty }), [isDirty])

  return (
    <FormDirtyContext.Provider value={value}>
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
