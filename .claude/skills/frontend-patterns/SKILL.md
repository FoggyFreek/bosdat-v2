---
name: frontend-patterns
description: "React component, hook, context, and page templates: feature structure, TanStack Query (useQuery/useMutation), shadcn/ui, form handling, loading/error states. Use when implementing new frontend features or components."
---

# Frontend Patterns

BosDAT uses **Bulletproof React** feature structure with TanStack Query + React Context.

| Layer | Role | Location |
|-------|------|---------|
| **Page** | Route entry, composes feature components | `features/[domain]/pages/` |
| **Component** | UI rendering, delegates logic to hooks | `features/[domain]/components/` |
| **Hook** | Data fetching, mutations, local state | `features/[domain]/hooks/` |
| **API** | Axios calls — no business logic | `features/[domain]/api.ts` |
| **Types** | DTOs and enums co-located | `features/[domain]/types.ts` |
| **Context** | Shared state across feature | `features/[domain]/context/` |

---

## API Module

```ts
// features/students/api.ts
import { api } from '@/services/api'
import type { Student, CreateStudentDto } from './types'

export const getStudents = () =>
  api.get<Student[]>('/students').then(r => r.data)

export const getStudentById = (id: number) =>
  api.get<Student>(`/students/${id}`).then(r => r.data)

export const createStudent = (dto: CreateStudentDto) =>
  api.post<Student>('/students', dto).then(r => r.data)

export const updateStudent = (id: number, dto: Partial<CreateStudentDto>) =>
  api.put<Student>(`/students/${id}`, dto).then(r => r.data)

export const deleteStudent = (id: number) =>
  api.delete(`/students/${id}`)
```

---

## Custom Hook (TanStack Query)

```ts
// features/students/hooks/useStudents.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getStudents, createStudent, deleteStudent } from '../api'
import type { CreateStudentDto } from '../types'

export function useStudents() {
  return useQuery({
    queryKey: ['students'],
    queryFn: getStudents,
  })
}

export function useStudentById(id: number) {
  return useQuery({
    queryKey: ['students', id],
    queryFn: () => getStudentById(id),
    enabled: !!id,
  })
}

export function useCreateStudent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createStudent,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
    },
  })
}

export function useDeleteStudent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteStudent,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
    },
  })
}
```

**Query key convention:**
```ts
['students']              // List
['students', id]          // Detail
['students', id, 'transactions'] // Related resource
```

---

## Component (with loading / error states)

```tsx
// features/students/components/StudentList.tsx
import { useStudents } from '../hooks/useStudents'
import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'

export function StudentList() {
  const { t } = useTranslation()
  const { data: students, isLoading, isError } = useStudents()

  if (isLoading) return <Skeleton className="h-48 w-full" />
  if (isError) return <p className="text-destructive">{t('common.errors.load')}</p>

  const list = students ?? []

  return (
    <ul className="space-y-2">
      {list.length === 0 && (
        <li className="text-muted-foreground">{t('students.empty')}</li>
      )}
      {list.map(student => (
        <StudentCard key={student.id} student={student} />
      ))}
    </ul>
  )
}
```

**Flat conditional rendering — keep scannable:**
```tsx
{isLoading && <Skeleton />}
{!isLoading && list.length === 0 && <EmptyState />}
{!isLoading && list.length > 0 && <DataList items={list} />}
```

---

## Form Component (shadcn/ui + react-hook-form)

```tsx
// features/students/components/CreateStudentForm.tsx
import { useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useCreateStudent } from '../hooks/useStudents'
import { useTranslation } from 'react-i18next'
import type { CreateStudentDto } from '../types'

export function CreateStudentForm() {
  const { t } = useTranslation()
  const { mutate, isPending } = useCreateStudent()
  const { register, handleSubmit, reset } = useForm<CreateStudentDto>()

  const onSubmit = (data: CreateStudentDto) => {
    mutate(data, { onSuccess: () => reset() })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1">
        <Label htmlFor="firstName">{t('students.form.firstName')}</Label>
        <Input id="firstName" {...register('firstName', { required: true })} />
      </div>

      <div className="space-y-1">
        <Label htmlFor="lastName">{t('students.form.lastName')}</Label>
        <Input id="lastName" {...register('lastName', { required: true })} />
      </div>

      <Button type="submit" disabled={isPending}>
        {isPending ? t('common.actions.saving') : t('common.actions.save')}
      </Button>
    </form>
  )
}
```

---

## Context (memoized — required)

```tsx
// features/auth/context/AuthContext.tsx
import { createContext, useContext, useState, useMemo, useCallback } from 'react'

interface AuthContextValue {
  user: User | null
  login: (credentials: LoginDto) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  const login = useCallback(async (credentials: LoginDto) => {
    const user = await authApi.login(credentials)
    setUser(user)
  }, [])

  const logout = useCallback(() => {
    setUser(null)
  }, [])

  // Memoize value — reference only changes when data changes
  const value = useMemo(
    () => ({ user, login, logout }),
    [user, login, logout]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
```

---

## Page (route entry, lazy-loaded)

```tsx
// features/students/pages/StudentsPage.tsx
import { StudentList } from '../components/StudentList'
import { CreateStudentForm } from '../components/CreateStudentForm'
import { useTranslation } from 'react-i18next'

export function StudentsPage() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{t('common.entities.students')}</h1>
      <CreateStudentForm />
      <StudentList />
    </div>
  )
}

// Route definition (lazy-loaded — direct import, NOT barrel)
// const StudentsPage = lazy(() => import('./features/students/pages/StudentsPage'))
```

---

## Types

```ts
// features/students/types.ts
// API enum values are always strings — never numbers
export type StudentStatus = 'Active' | 'Inactive' | 'Graduated'

export interface Student {
  id: number
  firstName: string
  lastName: string
  status: StudentStatus
  enrollments?: Enrollment[]
}

export interface CreateStudentDto {
  firstName: string
  lastName: string
}
```

---

## Rules

- UI in components, logic in hooks — no data fetching in components
- shadcn/ui + Tailwind only — no custom CSS or inline styles
- Always null-coalesce arrays: `const list = data ?? []`
- Stable list keys: `key={item.id}` never `key={index}`
- Memoize context values and callbacks (see Context section above)
- 200–400 lines per file; 800 max — split by concern if larger
