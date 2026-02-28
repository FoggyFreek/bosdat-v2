import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import { ManageUsersSection } from '../ManageUsersSection'
import { usersApi } from '@/features/users/api'
import type { PagedResult, UserListItem } from '@/features/users/types'

vi.mock('@/features/users/api', () => ({
  usersApi: {
    getUsers: vi.fn(),
    getUserById: vi.fn(),
    createUser: vi.fn(),
    updateDisplayName: vi.fn(),
    updateStatus: vi.fn(),
    resendInvitation: vi.fn(),
  },
  accountApi: {
    validateToken: vi.fn(),
    setPassword: vi.fn(),
  },
}))

const mockUsers: UserListItem[] = [
  {
    id: 'user-1',
    displayName: 'Alice Admin',
    email: 'alice@example.com',
    role: 'Admin',
    accountStatus: 'Active',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'user-2',
    displayName: 'Bob Teacher',
    email: 'bob@example.com',
    role: 'Teacher',
    accountStatus: 'PendingFirstLogin',
    createdAt: '2026-01-15T00:00:00Z',
  },
]

const mockPagedResult: PagedResult<UserListItem> = {
  items: mockUsers,
  totalCount: 2,
  page: 1,
  pageSize: 20,
}

const emptyPagedResult: PagedResult<UserListItem> = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
}

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  )
}

const renderWithProviders = (ui: ReactNode) => {
  const Wrapper = createWrapper()
  return render(<Wrapper>{ui}</Wrapper>)
}

describe('ManageUsersSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders card title and create button', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(mockPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('users.title')).toBeInTheDocument()
      })
      expect(screen.getByRole('button', { name: /users\.createUser/i })).toBeInTheDocument()
    })

    it('renders loading spinner initially', () => {
      vi.mocked(usersApi.getUsers).mockImplementation(() => new Promise(() => {}))

      renderWithProviders(<ManageUsersSection />)

      expect(document.querySelector('.animate-spin')).toBeInTheDocument()
    })

    it('renders user list after data loads', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(mockPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('Alice Admin')).toBeInTheDocument()
      })
      expect(screen.getByText('alice@example.com')).toBeInTheDocument()
      expect(screen.getByText('Bob Teacher')).toBeInTheDocument()
      expect(screen.getByText('bob@example.com')).toBeInTheDocument()
    })

    it('renders empty state when no users found', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(emptyPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('users.empty')).toBeInTheDocument()
      })
    })

    it('renders column headers', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(mockPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('users.fields.displayName')).toBeInTheDocument()
      })
      expect(screen.getByText('users.fields.email')).toBeInTheDocument()
      expect(screen.getByText('users.fields.role')).toBeInTheDocument()
      expect(screen.getByText('users.fields.status')).toBeInTheDocument()
    })
  })

  describe('filters', () => {
    it('renders search input', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(emptyPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByPlaceholderText('common.actions.search')).toBeInTheDocument()
      })
    })

    it('renders role filter select', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(emptyPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('users.filters.allRoles')).toBeInTheDocument()
      })
    })

    it('renders status filter select', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(emptyPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('users.filters.allStatuses')).toBeInTheDocument()
      })
    })
  })

  describe('invite dialog', () => {
    it('opens InviteUserDialog when Create User button is clicked', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(emptyPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /users\.createUser/i })).toBeInTheDocument()
      })

      screen.getByRole('button', { name: /users\.createUser/i }).click()

      await waitFor(() => {
        expect(screen.getByText('users.create.title')).toBeInTheDocument()
      })
    })
  })

  describe('pagination', () => {
    it('does not show pagination when total pages is 1', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue(mockPagedResult)

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByText('Alice Admin')).toBeInTheDocument()
      })

      expect(screen.queryByRole('button', { name: 'common.pagination.previous' })).not.toBeInTheDocument()
    })

    it('shows pagination when there are multiple pages', async () => {
      vi.mocked(usersApi.getUsers).mockResolvedValue({
        ...mockPagedResult,
        totalCount: 25,
        pageSize: 20,
      })

      renderWithProviders(<ManageUsersSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'common.pagination.previous' })).toBeInTheDocument()
      })
      expect(screen.getByRole('button', { name: 'common.pagination.next' })).toBeInTheDocument()
    })
  })
})
