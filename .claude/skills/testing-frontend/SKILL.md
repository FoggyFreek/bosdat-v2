---
name: testing-frontend
description: "Vitest + React Testing Library patterns: component test setup, QueryClient mocking, vi.mock for API modules, renderHook, userEvent. Use when writing or updating frontend tests."
---

# Frontend Testing

**Vitest 4** with **React Testing Library** and **userEvent**. 80% coverage minimum.

**Always import from `@/test/utils`**, never `@testing-library/react` — it wraps RTL with BrowserRouter + QueryClient.

---

## Test File Setup

```ts
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { StudentsPage } from '../StudentsPage'

// Mock the feature API module — one vi.mock per module
vi.mock('@/features/students/api', () => ({
  getStudents: vi.fn(),
  createStudent: vi.fn(),
}))

// Import mocked version for setup
import { getStudents, createStudent } from '@/features/students/api'
const mockGetStudents = vi.mocked(getStudents)
const mockCreateStudent = vi.mocked(createStudent)
```

---

## Component Test Structure

```ts
describe('StudentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders student list when data loads', async () => {
    // Arrange
    mockGetStudents.mockResolvedValue([
      { id: 1, firstName: 'Alice', lastName: 'Jansen' },
    ])

    // Act
    render(<StudentsPage />)

    // Assert
    await waitFor(() => {
      expect(screen.getByText('Alice Jansen')).toBeInTheDocument()
    })
  })

  it('renders empty state when no students', async () => {
    mockGetStudents.mockResolvedValue([])

    render(<StudentsPage />)

    await waitFor(() => {
      expect(screen.getByText('students.empty')).toBeInTheDocument()
    })
  })

  it('shows loading state initially', () => {
    mockGetStudents.mockResolvedValue([])
    render(<StudentsPage />)
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })
})
```

---

## QueryClient per Test

Each test gets a fresh QueryClient via the wrapper in `@/test/utils`. If you need direct QueryClient access:

```ts
import { createTestQueryClient } from '@/test/utils'

const queryClient = createTestQueryClient()

render(<Component />, { queryClient })

// After mutation, assert cache was invalidated
await waitFor(() => {
  expect(mockGetStudents).toHaveBeenCalledTimes(2) // initial + refetch
})
```

---

## User Interactions

```ts
it('creates a student on form submit', async () => {
  const user = userEvent.setup()
  mockCreateStudent.mockResolvedValue({ id: 2, firstName: 'Bob' })
  mockGetStudents.mockResolvedValue([])

  render(<CreateStudentForm />)

  await user.type(screen.getByLabelText('students.form.firstName'), 'Bob')
  await user.type(screen.getByLabelText('students.form.lastName'), 'Smit')
  await user.click(screen.getByRole('button', { name: 'common.actions.save' }))

  await waitFor(() => {
    expect(mockCreateStudent).toHaveBeenCalledWith({
      firstName: 'Bob',
      lastName: 'Smit',
    })
  })
})
```

---

## renderHook (custom hooks)

```ts
import { renderHook, waitFor } from '@/test/utils'
import { useStudents } from '../hooks/useStudents'

it('fetches students on mount', async () => {
  mockGetStudents.mockResolvedValue([{ id: 1, firstName: 'Alice' }])

  const { result } = renderHook(() => useStudents())

  await waitFor(() => {
    expect(result.current.isSuccess).toBe(true)
  })

  expect(result.current.data).toHaveLength(1)
})
```

---

## vi.mock Patterns

```ts
// Factory returning named exports
vi.mock('@/features/students/api', () => ({
  getStudents: vi.fn(),
  updateStudent: vi.fn(),
}))

// Namespace import (import * as X from '...')
vi.mock('@/features/students/api')
import * as studentsApi from '@/features/students/api'
vi.spyOn(studentsApi, 'getStudents').mockResolvedValue([])

// Module with default export
vi.mock('@/components/Chart', () => ({
  default: () => <div data-testid="chart-mock" />,
}))
```

**Rule:** Each module split into separate files needs its own `vi.mock()` call.

---

## Query Key Assertions

After mutations, verify query invalidation causes a refetch:

```ts
// Mutation triggers invalidation → refetch → API called again
await user.click(screen.getByRole('button', { name: 'common.actions.delete' }))

await waitFor(() => {
  expect(mockGetStudents).toHaveBeenCalledTimes(2)
})
```

---

## Rules

- Use `screen.getBy*` for elements that must be present; `queryBy*` for optional
- Use `findBy*` (async) instead of `getBy*` + `waitFor` for async elements
- Never use `act()` directly — RTL's `userEvent` and `waitFor` handle it
- Translation keys are mocked as-is: assert `'students.form.firstName'` not `'Voornaam'`
- Use `vi.clearAllMocks()` in `beforeEach`, not `afterEach`
- Never use array index as key; always assert on stable IDs when checking list items
- 200–400 lines typical per test file; split by feature/concern if larger
