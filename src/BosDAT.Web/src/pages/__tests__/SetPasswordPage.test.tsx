import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import { SetPasswordPage } from '../SetPasswordPage'
import { accountApi } from '@/features/users/api'
import type { ValidateTokenResponse } from '@/features/users/types'

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

const validTokenResponse: ValidateTokenResponse = {
  isValid: true,
  email: 'user@example.com',
}

const invalidTokenResponse: ValidateTokenResponse = {
  isValid: false,
  email: undefined,
}

const createWrapper = (initialEntries: string[] = ['/set-password']) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={initialEntries}
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        {children}
      </MemoryRouter>
    </QueryClientProvider>
  )
}

const renderWithProviders = (ui: ReactNode, initialEntries?: string[]) => {
  const Wrapper = createWrapper(initialEntries)
  return render(<Wrapper>{ui}</Wrapper>)
}

describe('SetPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    window.location.hash = '#token=abc123'
  })

  describe('no token in URL', () => {
    it('shows invalid token card when no token provided', async () => {
      window.location.hash = ''
      renderWithProviders(<SetPasswordPage />, ['/set-password'])

      await waitFor(() => {
        expect(screen.getByText('setPassword.invalidToken')).toBeInTheDocument()
      })
      expect(screen.getByText('setPassword.contactAdmin')).toBeInTheDocument()
    })

    it('does not call validateToken when no token', () => {
      window.location.hash = ''
      renderWithProviders(<SetPasswordPage />, ['/set-password'])

      expect(accountApi.validateToken).not.toHaveBeenCalled()
    })
  })

  describe('loading state', () => {
    it('shows loading spinner while validating token', () => {
      vi.mocked(accountApi.validateToken).mockImplementation(() => new Promise(() => {}))

      renderWithProviders(<SetPasswordPage />)

      expect(document.querySelector('.animate-spin')).toBeInTheDocument()
    })
  })

  describe('invalid token', () => {
    it('shows error card when token is invalid', async () => {
      vi.mocked(accountApi.validateToken).mockResolvedValue(invalidTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByText('setPassword.invalidToken')).toBeInTheDocument()
      })
      expect(screen.getByText('setPassword.contactAdmin')).toBeInTheDocument()
    })

    it('does not render password form for invalid token', async () => {
      vi.mocked(accountApi.validateToken).mockResolvedValue(invalidTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByText('setPassword.invalidToken')).toBeInTheDocument()
      })

      expect(screen.queryByText('setPassword.title')).not.toBeInTheDocument()
    })
  })

  describe('valid token', () => {
    it('renders password form with title and subtitle', async () => {
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByText('setPassword.title')).toBeInTheDocument()
      })
      expect(screen.getByText('setPassword.subtitle')).toBeInTheDocument()
    })

    it('renders email field pre-filled and read-only', async () => {
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByDisplayValue('user@example.com')).toBeInTheDocument()
      })
      expect(screen.getByDisplayValue('user@example.com')).toHaveAttribute('readonly')
    })

    it('renders password and confirm password fields', async () => {
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('setPassword.password')).toBeInTheDocument()
      })
      expect(screen.getByLabelText('setPassword.confirmPassword')).toBeInTheDocument()
    })

    it('renders submit button', async () => {
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'setPassword.submit' })).toBeInTheDocument()
      })
    })
  })

  describe('form validation', () => {
    it('shows required error when password is empty', async () => {
      const user = userEvent.setup()
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'setPassword.submit' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'setPassword.submit' }))

      await waitFor(() => {
        expect(screen.getByText('common.validation.required')).toBeInTheDocument()
      })
    })

    it('shows error when password is too short', async () => {
      const user = userEvent.setup()
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('setPassword.password')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('setPassword.password'), 'abc')
      await user.click(screen.getByRole('button', { name: 'setPassword.submit' }))

      await waitFor(() => {
        expect(screen.getByText('setPassword.passwordTooShort')).toBeInTheDocument()
      })
    })

    it('shows error when password does not meet complexity requirements', async () => {
      const user = userEvent.setup()
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('setPassword.password')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('setPassword.password'), 'alllowercase1')
      await user.click(screen.getByRole('button', { name: 'setPassword.submit' }))

      await waitFor(() => {
        expect(screen.getByText('setPassword.passwordRequirementsNotMet')).toBeInTheDocument()
      })
    })

    it('shows error when passwords do not match', async () => {
      const user = userEvent.setup()
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('setPassword.password')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('setPassword.password'), 'Password1')
      await user.type(screen.getByLabelText('setPassword.confirmPassword'), 'Password2')
      await user.click(screen.getByRole('button', { name: 'setPassword.submit' }))

      await waitFor(() => {
        expect(screen.getByText('setPassword.passwordMismatch')).toBeInTheDocument()
      })
    })
  })

  describe('successful submission', () => {
    it('calls setPassword with correct payload', async () => {
      const user = userEvent.setup()
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)
      vi.mocked(accountApi.setPassword).mockResolvedValue(undefined)

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('setPassword.password')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('setPassword.password'), 'SecurePass1!')
      await user.type(screen.getByLabelText('setPassword.confirmPassword'), 'SecurePass1!')
      await user.click(screen.getByRole('button', { name: 'setPassword.submit' }))

      await waitFor(() => {
        expect(accountApi.setPassword).toHaveBeenCalledWith({
          token: 'abc123',
          password: 'SecurePass1!',
        })
      })
    })
  })

  describe('error state', () => {
    it('shows unexpected error message when setPassword fails', async () => {
      const user = userEvent.setup()
      vi.mocked(accountApi.validateToken).mockResolvedValue(validTokenResponse)
      vi.mocked(accountApi.setPassword).mockRejectedValue(new Error('Server error'))

      renderWithProviders(<SetPasswordPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('setPassword.password')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText('setPassword.password'), 'SecurePass1!')
      await user.type(screen.getByLabelText('setPassword.confirmPassword'), 'SecurePass1!')
      await user.click(screen.getByRole('button', { name: 'setPassword.submit' }))

      await waitFor(() => {
        expect(screen.getByText('common.errors.unexpected')).toBeInTheDocument()
      })
    })
  })
})
