import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import { InviteUserDialog } from '../InviteUserDialog'
import { usersApi } from '@/features/users/api'
import type { InvitationResponse } from '@/features/users/types'

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

vi.mock('@/features/teachers/api', () => ({
  teachersApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    getWithCourses: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}))

vi.mock('@/features/students/api', () => ({
  studentsApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockInvitationResponse: InvitationResponse = {
  userId: 'new-user-id',
  invitationUrl: 'http://localhost:5173/set-password?token=abc123',
  expiresAt: '2026-03-03T12:00:00Z',
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

describe('InviteUserDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('when open', () => {
    it('renders dialog title and description', async () => {
      renderWithProviders(
        <InviteUserDialog open={true} onOpenChange={vi.fn()} />
      )

      await waitFor(() => {
        expect(screen.getByText('users.create.title')).toBeInTheDocument()
      })
      expect(screen.getByText('users.create.description')).toBeInTheDocument()
    })

    it('renders form fields', async () => {
      renderWithProviders(
        <InviteUserDialog open={true} onOpenChange={vi.fn()} />
      )

      await waitFor(() => {
        expect(screen.getByLabelText('users.fields.role')).toBeInTheDocument()
      })
      expect(screen.getByLabelText('users.fields.displayName')).toBeInTheDocument()
      expect(screen.getByLabelText('users.fields.email')).toBeInTheDocument()
    })

    it('renders Invite and Cancel buttons', async () => {
      renderWithProviders(
        <InviteUserDialog open={true} onOpenChange={vi.fn()} />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'users.inviteUser' })).toBeInTheDocument()
      })
      expect(screen.getByRole('button', { name: 'common.actions.cancel' })).toBeInTheDocument()
    })
  })

  describe('with locked role (from profile page)', () => {
    it('renders locked role as readonly input', async () => {
      renderWithProviders(
        <InviteUserDialog
          open={true}
          onOpenChange={vi.fn()}
          lockedRole="Teacher"
          linkedObjectId="teacher-1"
          linkedObjectType="Teacher"
          defaultEmail="teacher@example.com"
          defaultDisplayName="John Smith"
        />
      )

      await waitFor(() => {
        const roleInput = screen.getByDisplayValue('users.roles.Teacher')
        expect(roleInput).toBeInTheDocument()
        expect(roleInput).toHaveAttribute('readonly')
      })
    })

    it('pre-fills email and display name from props', async () => {
      renderWithProviders(
        <InviteUserDialog
          open={true}
          onOpenChange={vi.fn()}
          lockedRole="Teacher"
          linkedObjectId="teacher-1"
          linkedObjectType="Teacher"
          defaultEmail="teacher@example.com"
          defaultDisplayName="John Smith"
        />
      )

      await waitFor(() => {
        expect(screen.getByDisplayValue('teacher@example.com')).toBeInTheDocument()
      })
      expect(screen.getByDisplayValue('John Smith')).toBeInTheDocument()
    })

    it('does not render linked entity picker when linkedObjectId is preset', async () => {
      renderWithProviders(
        <InviteUserDialog
          open={true}
          onOpenChange={vi.fn()}
          lockedRole="Teacher"
          linkedObjectId="teacher-1"
          linkedObjectType="Teacher"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('users.create.title')).toBeInTheDocument()
      })

      // Should not see the teacher/student select label
      expect(screen.queryByText('common.entities.teacher')).not.toBeInTheDocument()
    })
  })

  describe('validation', () => {
    it('shows required errors when submitting empty form', async () => {
      const user = userEvent.setup()
      renderWithProviders(
        <InviteUserDialog open={true} onOpenChange={vi.fn()} />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'users.inviteUser' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'users.inviteUser' }))

      await waitFor(() => {
        expect(screen.getAllByText('common.validation.required').length).toBeGreaterThan(0)
      })
    })

    it('shows invalid email error for bad email format', async () => {
      const user = userEvent.setup()
      renderWithProviders(
        <InviteUserDialog open={true} onOpenChange={vi.fn()} />
      )

      await waitFor(() => {
        expect(screen.getByLabelText('users.fields.email')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('users.fields.email'), 'not-an-email')
      await user.type(screen.getByLabelText('users.fields.displayName'), 'Some Name')
      await user.click(screen.getByRole('button', { name: 'users.inviteUser' }))

      await waitFor(() => {
        expect(screen.getByText('common.validation.invalidEmail')).toBeInTheDocument()
      })
    })
  })

  describe('successful submission', () => {
    it('shows success state with invitation URL after creating user', async () => {
      const user = userEvent.setup()
      vi.mocked(usersApi.createUser).mockResolvedValue(mockInvitationResponse)

      renderWithProviders(
        <InviteUserDialog
          open={true}
          onOpenChange={vi.fn()}
          lockedRole="Admin"
          defaultEmail="newadmin@example.com"
          defaultDisplayName="New Admin"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'users.inviteUser' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'users.inviteUser' }))

      await waitFor(() => {
        expect(screen.getByText('users.create.success')).toBeInTheDocument()
      })
      expect(screen.getByDisplayValue(mockInvitationResponse.invitationUrl)).toBeInTheDocument()
    })

    it('calls createUser with correct payload', async () => {
      const user = userEvent.setup()
      vi.mocked(usersApi.createUser).mockResolvedValue(mockInvitationResponse)

      renderWithProviders(
        <InviteUserDialog
          open={true}
          onOpenChange={vi.fn()}
          lockedRole="FinancialAdmin"
          defaultEmail="finance@example.com"
          defaultDisplayName="Finance User"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'users.inviteUser' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'users.inviteUser' }))

      await waitFor(() => {
        expect(usersApi.createUser).toHaveBeenCalledWith(
          expect.objectContaining({
            role: 'FinancialAdmin',
            email: 'finance@example.com',
            displayName: 'Finance User',
          })
        )
      })
    })
  })

  describe('error state', () => {
    it('shows error message when creation fails', async () => {
      const user = userEvent.setup()
      vi.mocked(usersApi.createUser).mockRejectedValue(new Error('Conflict'))

      renderWithProviders(
        <InviteUserDialog
          open={true}
          onOpenChange={vi.fn()}
          lockedRole="Admin"
          defaultEmail="admin@example.com"
          defaultDisplayName="Admin User"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'users.inviteUser' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'users.inviteUser' }))

      await waitFor(() => {
        expect(screen.getByText('common.errors.unexpected')).toBeInTheDocument()
      })
    })
  })

  describe('closed state', () => {
    it('does not render dialog content when closed', () => {
      renderWithProviders(
        <InviteUserDialog open={false} onOpenChange={vi.fn()} />
      )

      expect(screen.queryByText('users.create.title')).not.toBeInTheDocument()
    })
  })
})
