# Frontend Rules (React 19 / TypeScript)

## Naming
- Components/Types: PascalCase | Functions/vars: camelCase | CSS: kebab-case
- Structure: `features/[domain]/` → components, types, context
- Generic components: `components/[component]`

## API enum properties
- API returns enum values as strings, never numbers. Check the relevant API controller before writing code.

## Immutability
Never mutate existing objects or arrays — always create and use a new copy when updating state or data.

## Code Splitting
- Use direct imports with `lazy(() => import('./pages/StudentsPage'))` (separate chunk)
- Never barrel imports: `lazy(() => import('./pages').then(...))` (collapses to single chunk)

## Context Memoization (REQUIRED)
Memoize context values and callbacks so references only change when data changes, preventing unnecessary re-renders.

## Null Safety
Always use fallbacks for arrays: `const items = data.items ?? []`

## Conditional Rendering
Keep flat, avoid deep nesting — prefer early returns or simple expressions over nested ternaries.

## Keys & Accessibility
- Lists: Stable IDs only — `key={item.id}`, never `key={i}`
- Interactive divs: Add `role="button"`, `tabIndex={0}`, `onKeyDown` (Enter/Space)

## React Router
**Required:** `<BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>`
**Locations:** `main.tsx`, `test/utils.tsx`, all test BrowserRouters

## Testing
- Import from `@/test/utils` (NOT `@testing-library/react`)
- Fresh `QueryClient` per test (`createTestQueryClient()`)
- Co-locate in `__tests__/` folders
- `react-i18next` globally mocked in `test/setup.ts` - translation keys returned as-is
- NEVER import `@/i18n/config` in test utils (breaks mock hoisting)
- Use `import type { TFunction } from 'i18next'` for typing `t` function parameters

## Component Rules
- UI in components, logic in hooks
- shadcn/ui + Tailwind only (no custom CSS/inline styles)
- 200-400 lines typical, 800 max

Use the relevant skill (`frontend-patterns`, `testing-frontend`, `i18n`) when implementing components, writing tests, or adding translations.
