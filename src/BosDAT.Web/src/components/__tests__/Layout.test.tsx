import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { Layout } from '../Layout'

const mockNavigate = vi.fn()
const mockLogout = vi.fn().mockResolvedValue(undefined)

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

vi.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: { firstName: 'John', lastName: 'Doe', email: 'john@test.com' },
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    logout: mockLogout,
  }),
}))

vi.mock('@/hooks/useSchoolName', () => ({
  useSchoolName: () => ({
    schoolName: 'Test Music Academy',
    isLoading: false,
  }),
}))

describe('Layout', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('displays school name in top bar', () => {
    render(<Layout>content</Layout>)
    expect(screen.getByText('Test Music Academy')).toBeInTheDocument()
  })

  it('renders disabled search input', () => {
    render(<Layout>content</Layout>)
    const searchInput = screen.getByPlaceholderText('Search...')
    expect(searchInput).toBeDisabled()
  })

  it('renders sign out button with title', () => {
    render(<Layout>content</Layout>)
    const signOutBtn = screen.getByTitle('Sign out')
    expect(signOutBtn).toBeInTheDocument()
  })

  it('renders settings button with title', () => {
    render(<Layout>content</Layout>)
    const settingsBtn = screen.getByTitle('Settings')
    expect(settingsBtn).toBeInTheDocument()
  })

  it('renders user avatar with full name title', () => {
    render(<Layout>content</Layout>)
    const avatarBtn = screen.getByTitle('John Doe')
    expect(avatarBtn).toBeInTheDocument()
  })

  it('renders user initials in avatar', () => {
    render(<Layout>content</Layout>)
    expect(screen.getByText('JD')).toBeInTheDocument()
  })

  it('calls logout and navigates on sign out click', async () => {
    render(<Layout>content</Layout>)
    const signOutBtn = screen.getByTitle('Sign out')
    await fireEvent.click(signOutBtn)
    expect(mockLogout).toHaveBeenCalled()
    expect(mockNavigate).toHaveBeenCalledWith('/login')
  })

  it('navigates to /settings on settings button click', () => {
    render(<Layout>content</Layout>)
    const settingsBtn = screen.getByTitle('Settings')
    fireEvent.click(settingsBtn)
    expect(mockNavigate).toHaveBeenCalledWith('/settings')
  })

  it('navigates to /settings on avatar click', () => {
    render(<Layout>content</Layout>)
    const avatarBtn = screen.getByTitle('John Doe')
    fireEvent.click(avatarBtn)
    expect(mockNavigate).toHaveBeenCalledWith('/settings')
  })

  it('does not include Settings in sidebar navigation', () => {
    render(<Layout>content</Layout>)
    const navLinks = screen.getAllByRole('link')
    const navNames = navLinks.map((link) => link.textContent)
    expect(navNames).not.toContain('Settings')
  })

  it('includes expected sidebar navigation items', () => {
    render(<Layout>content</Layout>)
    expect(screen.getByText('Dashboard')).toBeInTheDocument()
    expect(screen.getByText('Students')).toBeInTheDocument()
    expect(screen.getByText('Teachers')).toBeInTheDocument()
    expect(screen.getByText('Courses')).toBeInTheDocument()
    expect(screen.getByText('Schedule')).toBeInTheDocument()
    expect(screen.getByText('Invoices')).toBeInTheDocument()
  })

  it('renders children content', () => {
    render(<Layout><div>Test Content</div></Layout>)
    expect(screen.getByText('Test Content')).toBeInTheDocument()
  })
})
