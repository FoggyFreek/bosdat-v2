import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { GlobalSearch } from '../GlobalSearch'
import type { SearchResult } from '@/hooks/useGlobalSearch'

const mockClose = vi.fn()
const mockSetTerm = vi.fn()
const mockSetActiveIndex = vi.fn()
const mockOnSelect = vi.fn()

const defaultHookReturn = {
  term: '',
  setTerm: mockSetTerm,
  debouncedTerm: '',
  isOpen: false,
  close: mockClose,
  results: [] as SearchResult[],
  isLoading: false,
  activeIndex: -1,
  setActiveIndex: mockSetActiveIndex,
  onSelect: mockOnSelect,
}

let hookReturn = { ...defaultHookReturn }

vi.mock('@/hooks/useGlobalSearch', () => ({
  useGlobalSearch: () => hookReturn,
}))

describe('GlobalSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    hookReturn = { ...defaultHookReturn }
  })

  it('renders a search input with placeholder', () => {
    render(<GlobalSearch />)
    expect(screen.getByPlaceholderText('search.placeholder')).toBeInTheDocument()
  })

  it('does not show dropdown when isOpen is false', () => {
    render(<GlobalSearch />)
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('shows min characters hint when term is short', () => {
    hookReturn = { ...defaultHookReturn, isOpen: true, term: 'a', debouncedTerm: 'a' }
    render(<GlobalSearch />)
    expect(screen.getByText('search.minCharacters')).toBeInTheDocument()
  })

  it('shows loading state', () => {
    hookReturn = { ...defaultHookReturn, isOpen: true, term: 'test', debouncedTerm: 'test', isLoading: true }
    render(<GlobalSearch />)
    expect(screen.getByText('common.states.loading')).toBeInTheDocument()
  })

  it('shows no results message', () => {
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'xyz',
      debouncedTerm: 'xyz',
      results: [],
      isLoading: false,
    }
    render(<GlobalSearch />)
    expect(screen.getByText('search.noResults')).toBeInTheDocument()
  })

  it('shows grouped results with headers', () => {
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      results: [
        { id: 's1', type: 'student', name: 'Alice Smith', subtitle: 'alice@test.com' },
        { id: 't1', type: 'teacher', name: 'Alan Teacher', subtitle: 'alan@test.com' },
      ],
    }
    render(<GlobalSearch />)
    expect(screen.getByText('common.entities.students')).toBeInTheDocument()
    expect(screen.getByText('common.entities.teachers')).toBeInTheDocument()
    expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    expect(screen.getByText('Alan Teacher')).toBeInTheDocument()
  })

  it('shows only student header when no teacher results', () => {
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      results: [
        { id: 's1', type: 'student', name: 'Alice Smith', subtitle: 'alice@test.com' },
      ],
    }
    render(<GlobalSearch />)
    expect(screen.getByText('common.entities.students')).toBeInTheDocument()
    expect(screen.queryByText('common.entities.teachers')).not.toBeInTheDocument()
  })

  it('calls onSelect when clicking a result', () => {
    const studentResult: SearchResult = { id: 's1', type: 'student', name: 'Alice Smith', subtitle: 'alice@test.com' }
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      results: [studentResult],
    }
    render(<GlobalSearch />)

    fireEvent.click(screen.getByText('Alice Smith'))
    expect(mockOnSelect).toHaveBeenCalledWith(studentResult)
  })

  it('calls setTerm on input change', () => {
    render(<GlobalSearch />)
    const input = screen.getByPlaceholderText('search.placeholder')
    fireEvent.change(input, { target: { value: 'test' } })
    expect(mockSetTerm).toHaveBeenCalledWith('test')
  })

  it('calls close on Escape key', () => {
    hookReturn = { ...defaultHookReturn, isOpen: true, term: 'test', debouncedTerm: 'test' }
    render(<GlobalSearch />)
    const input = screen.getByPlaceholderText('search.placeholder')
    fireEvent.keyDown(input, { key: 'Escape' })
    expect(mockClose).toHaveBeenCalled()
  })

  it('navigates results with ArrowDown', () => {
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      activeIndex: -1,
      results: [
        { id: 's1', type: 'student', name: 'Alice', subtitle: 'a@test.com' },
        { id: 't1', type: 'teacher', name: 'Alan', subtitle: 'b@test.com' },
      ],
    }
    render(<GlobalSearch />)
    const input = screen.getByPlaceholderText('search.placeholder')
    fireEvent.keyDown(input, { key: 'ArrowDown' })
    expect(mockSetActiveIndex).toHaveBeenCalledWith(0)
  })

  it('navigates results with ArrowUp from index 1', () => {
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      activeIndex: 1,
      results: [
        { id: 's1', type: 'student', name: 'Alice', subtitle: 'a@test.com' },
        { id: 't1', type: 'teacher', name: 'Alan', subtitle: 'b@test.com' },
      ],
    }
    render(<GlobalSearch />)
    const input = screen.getByPlaceholderText('search.placeholder')
    fireEvent.keyDown(input, { key: 'ArrowUp' })
    expect(mockSetActiveIndex).toHaveBeenCalledWith(0)
  })

  it('selects active result on Enter', () => {
    const results: SearchResult[] = [
      { id: 's1', type: 'student', name: 'Alice', subtitle: 'a@test.com' },
    ]
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      activeIndex: 0,
      results,
    }
    render(<GlobalSearch />)
    const input = screen.getByPlaceholderText('search.placeholder')
    fireEvent.keyDown(input, { key: 'Enter' })
    expect(mockOnSelect).toHaveBeenCalledWith(results[0])
  })

  it('shows subtitle for each result', () => {
    hookReturn = {
      ...defaultHookReturn,
      isOpen: true,
      term: 'al',
      debouncedTerm: 'al',
      results: [
        { id: 's1', type: 'student', name: 'Alice Smith', subtitle: 'alice@test.com' },
      ],
    }
    render(<GlobalSearch />)
    expect(screen.getByText('alice@test.com')).toBeInTheDocument()
  })
})
