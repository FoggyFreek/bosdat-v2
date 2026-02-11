import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import { SchedulingSection } from '../SchedulingSection'
import { schedulingApi } from '@/features/settings/api'
import type { SchedulingStatus, ScheduleRunsResponse, ManualRunResult } from '@/features/settings/types'

vi.mock('@/features/settings/api', () => ({
  schedulingApi: {
    getStatus: vi.fn(),
    getRuns: vi.fn(),
    runManual: vi.fn(),
  },
}))

const mockStatus: SchedulingStatus = {
  lastScheduledDate: '2026-05-08',
  daysAhead: 90,
  activeCourseCount: 12,
}

const mockRunsPage1: ScheduleRunsResponse = {
  items: [
    {
      id: '1',
      startDate: '2026-02-07',
      endDate: '2026-05-08',
      totalCoursesProcessed: 12,
      totalLessonsCreated: 48,
      totalLessonsSkipped: 3,
      skipHolidays: true,
      status: 'Success',
      errorMessage: null,
      initiatedBy: 'Manual',
      createdAt: '2026-02-07T10:00:00Z',
    },
    {
      id: '2',
      startDate: '2026-01-01',
      endDate: '2026-04-01',
      totalCoursesProcessed: 10,
      totalLessonsCreated: 40,
      totalLessonsSkipped: 2,
      skipHolidays: true,
      status: 'Failed',
      errorMessage: 'Something went wrong',
      initiatedBy: 'Automatic',
      createdAt: '2026-01-01T08:00:00Z',
    },
  ],
  totalCount: 3,
  page: 1,
  pageSize: 5,
}

const mockRunsPage2: ScheduleRunsResponse = {
  items: [
    {
      id: '3',
      startDate: '2025-12-01',
      endDate: '2026-03-01',
      totalCoursesProcessed: 8,
      totalLessonsCreated: 32,
      totalLessonsSkipped: 0,
      skipHolidays: false,
      status: 'Success',
      errorMessage: null,
      initiatedBy: 'Manual',
      createdAt: '2025-12-01T09:00:00Z',
    },
  ],
  totalCount: 3,
  page: 2,
  pageSize: 5,
}

const mockManualResult: ManualRunResult = {
  scheduleRunId: '4',
  startDate: '2026-02-07',
  endDate: '2026-05-08',
  totalCoursesProcessed: 12,
  totalLessonsCreated: 50,
  totalLessonsSkipped: 5,
  status: 'Success',
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

describe('SchedulingSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(schedulingApi.getStatus).mockResolvedValue(mockStatus)
    vi.mocked(schedulingApi.getRuns).mockResolvedValue(mockRunsPage1)
    vi.mocked(schedulingApi.runManual).mockResolvedValue(mockManualResult)
  })

  describe('rendering', () => {
    it('renders card with title and description', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.title')).toBeInTheDocument()
      })
      expect(screen.getByText('settings.scheduling.description')).toBeInTheDocument()
    })

    it('renders loading state initially', () => {
      vi.mocked(schedulingApi.getStatus).mockImplementation(() => new Promise(() => {}))
      vi.mocked(schedulingApi.getRuns).mockImplementation(() => new Promise(() => {}))

      renderWithProviders(<SchedulingSection />)

      expect(screen.getByText('settings.scheduling.loading')).toBeInTheDocument()
    })

    it('renders status card after loading', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.statusCard.title')).toBeInTheDocument()
      })
      expect(screen.getByText('2026-05-08')).toBeInTheDocument()
      expect(screen.getByText(/90 common\.time\.day/)).toBeInTheDocument()
      expect(screen.getByText('12')).toBeInTheDocument()
    })

    it('renders "No lessons scheduled" when lastScheduledDate is null', async () => {
      vi.mocked(schedulingApi.getStatus).mockResolvedValue({
        ...mockStatus,
        lastScheduledDate: null,
      })

      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.statusCard.noLessons')).toBeInTheDocument()
      })
    })
  })

  describe('run history', () => {
    it('renders schedule run history', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('2026-02-07 - 2026-05-08')).toBeInTheDocument()
      })
      expect(screen.getByText('2026-01-01 - 2026-04-01')).toBeInTheDocument()
    })

    it('renders success and failed badges', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('Success')).toBeInTheDocument()
      })
      expect(screen.getByText('Failed')).toBeInTheDocument()
    })

    it('renders lesson counts', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('48 created, 3 skipped')).toBeInTheDocument()
      })
      expect(screen.getByText('40 created, 2 skipped')).toBeInTheDocument()
    })

    it('shows Load more button when more runs available', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'settings.scheduling.runHistory.loadMore' })).toBeInTheDocument()
      })
    })

    it('does not show Load more when all runs are loaded', async () => {
      vi.mocked(schedulingApi.getRuns).mockResolvedValue({
        ...mockRunsPage1,
        totalCount: 2,
      })

      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.runHistory.title')).toBeInTheDocument()
      })
      expect(screen.queryByRole('button', { name: 'settings.scheduling.runHistory.loadMore' })).not.toBeInTheDocument()
    })

    it('loads more runs when Load more is clicked', async () => {
      vi.mocked(schedulingApi.getRuns)
        .mockResolvedValueOnce(mockRunsPage1)
        .mockResolvedValueOnce(mockRunsPage2)

      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'settings.scheduling.runHistory.loadMore' })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: 'settings.scheduling.runHistory.loadMore' }))

      await waitFor(() => {
        expect(schedulingApi.getRuns).toHaveBeenCalledWith(2, 5)
      })
    })

    it('renders empty state when no runs exist', async () => {
      vi.mocked(schedulingApi.getRuns).mockResolvedValue({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 5,
      })

      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.runHistory.noRuns')).toBeInTheDocument()
      })
    })
  })

  describe('admin area', () => {
    it('renders admin area with manual run button', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.adminArea.title')).toBeInTheDocument()
      })
      expect(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i })).toBeInTheDocument()
    })

    it('shows confirmation dialog when manual run button is clicked', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i }))

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.adminArea.confirmTitle')).toBeInTheDocument()
      })
      expect(screen.getByText('settings.scheduling.adminArea.confirmDescription')).toBeInTheDocument()
    })

    it('calls runManual when confirmed', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i }))

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.adminArea.confirmTitle')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.confirmButton/i }))

      await waitFor(() => {
        expect(schedulingApi.runManual).toHaveBeenCalled()
      })
    })

    it('shows success result after manual run', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i }))

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.adminArea.confirmTitle')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.confirmButton/i }))

      await waitFor(() => {
        expect(screen.getByText(/settings\.scheduling\.adminArea\.manualRun/i)).toBeInTheDocument()
      })
      expect(screen.getByText(/settings\.scheduling\.adminArea\.result/i)).toBeInTheDocument()
    })

    it('shows failure result when manual run fails', async () => {
      vi.mocked(schedulingApi.runManual).mockRejectedValue(new Error('Server error'))

      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i }))

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.adminArea.confirmTitle')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.confirmButton/i }))

      await waitFor(() => {
        expect(screen.getByText(/settings\.scheduling\.adminArea\.manualRun/i)).toBeInTheDocument()
      })
    })

    it('closes confirmation dialog when Cancel is clicked', async () => {
      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /settings\.scheduling\.adminArea\.runButton/i }))

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.adminArea.confirmTitle')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: 'common.actions.cancel' }))

      await waitFor(() => {
        expect(screen.queryByText('settings.scheduling.adminArea.confirmTitle')).not.toBeInTheDocument()
      })
    })
  })

  describe('error state', () => {
    it('renders error state when status fetch fails', async () => {
      vi.mocked(schedulingApi.getStatus).mockRejectedValue(new Error('Forbidden'))

      renderWithProviders(<SchedulingSection />)

      await waitFor(() => {
        expect(screen.getByText('settings.scheduling.errorState')).toBeInTheDocument()
      })
    })
  })
})
