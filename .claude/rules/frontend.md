# Frontend Rules (React 18 / TypeScript)

## Naming
- Components/Types: PascalCase | Functions/vars: camelCase | CSS: kebab-case
- Structure: `features/[domain]/` → components, types, context

## Immutability (CRITICAL)
```ts
// ES2023: Use toSorted/toReversed/toSpliced (returns new array)
arr.toSorted() // ✅  |  arr.sort() // ❌ Mutates
return { ...user, name: newName } // ✅  |  user.name = newName // ❌
```

## Code Splitting
```tsx
// ✅ Direct import (separate chunk)
lazy(() => import('./pages/StudentsPage'))

// ❌ Barrel import (single chunk)
lazy(() => import('./pages').then(m => ({ default: m.StudentsPage })))
```

## Context Memoization (REQUIRED)
```tsx
const login = useCallback(async (data) => { ... }, [])
const value = useMemo(() => ({ user, login }), [user, login])
<AuthContext.Provider value={value}>
```

## TanStack Query
**Keys:** `['students']` → `['students', id]` → `['students', id, 'ledger']`
**Defaults:** staleTime: 5min, retry: 1
**Always:** `const { data: items = [] } = useQuery(...)` + `invalidateQueries` after mutations

## Null Safety (CRITICAL)
```tsx
// Arrays: Always use fallbacks
const students = step2.students ?? []
{students.map(s => <Card key={s.id} />)}

// Query defaults
const { data: rooms = [] } = useQuery<Room[]>(...)

// Props/destructuring
const { students = [] } = formData.step2
const MyComponent = ({ students = [], isLoading = false }) => { ... }

// Nested access
const courses = student?.enrollments?.[0]?.courses ?? []

// State updates
students: [...(state.formData.step2.students ?? []), action.payload]

// Prefer ?? over || (preserves 0, '', false)
const count = data.count ?? 0
```

## Conditional Rendering
```tsx
// ✅ Flat (scannable)
{isLoading && <Spinner />}
{!isLoading && items.length === 0 && <Empty />}
{!isLoading && items.length > 0 && <List items={items} />}

// ❌ Nested ternaries
{isLoading ? <Spinner /> : items.length === 0 ? <Empty /> : <List />}

// Extract complex logic
const getVariant = (s: string) => s === 'Active' ? 'default' : s === 'Trial' ? 'secondary' : 'outline'
```

## Keys & Accessibility
- **Lists:** Stable IDs, never index: `key={item.id}` not `key={i}`
- **Interactive divs:** Add `role="button"`, `tabIndex={0}`, `onKeyDown` (Enter/Space)

## React Router
**Required:** `<BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>`
**Locations:** `main.tsx`, `test/utils.tsx`, all test BrowserRouters

## Testing
- Import from `@/test/utils` (NOT `@testing-library/react`)
- Fresh `QueryClient` per test (`createTestQueryClient()`)
- Co-locate in `__tests__/` folders

## Component Rules
- Named exports, arrow functions: `const Component = () => { }`
- UI in components, logic in hooks
- shadcn/ui + Tailwind only (no custom CSS/inline styles)
- 200-400 lines typical, 800 max
