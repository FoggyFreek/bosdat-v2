import { describe, it, expect} from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { PricingHistoryCollapsible } from '../PricingHistoryCollapsible'
import type { CourseTypePricingVersion } from '@/features/course-types/types'

const mockCurrentVersion: CourseTypePricingVersion = {
  id: '1',
  courseTypeId: 'ct-1',
  priceAdult: 50,
  priceChild: 40,
  validFrom: '2024-01-01',
  validUntil: null,
  isCurrent: true,
  createdAt: '2024-01-01T00:00:00Z',
}

const mockHistoricVersion: CourseTypePricingVersion = {
  id: '2',
  courseTypeId: 'ct-1',
  priceAdult: 45,
  priceChild: 35,
  validFrom: '2023-01-01',
  validUntil: '2023-12-31',
  isCurrent: false,
  createdAt: '2023-01-01T00:00:00Z',
}

const mockOlderHistoricVersion: CourseTypePricingVersion = {
  id: '3',
  courseTypeId: 'ct-1',
  priceAdult: 40,
  priceChild: 30,
  validFrom: '2022-01-01',
  validUntil: '2022-12-31',
  isCurrent: false,
  createdAt: '2022-01-01T00:00:00Z',
}

describe('PricingHistoryCollapsible', () => {
  it('renders nothing when pricing history has only one version', () => {
    const { container } = render(
      <PricingHistoryCollapsible pricingHistory={[mockCurrentVersion]} />
    )

    expect(container.firstChild).toBeNull()
  })

  it('renders nothing when there are no historic versions', () => {
    const { container } = render(
      <PricingHistoryCollapsible pricingHistory={[mockCurrentVersion]} />
    )

    expect(container.firstChild).toBeNull()
  })

  it('renders collapsible trigger with correct count for single historic version', () => {
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion]}
      />
    )

    expect(screen.getByText('1 historic pricing version')).toBeInTheDocument()
  })

  it('renders collapsible trigger with correct count for multiple historic versions', () => {
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion, mockOlderHistoricVersion]}
      />
    )

    expect(screen.getByText('2 historic pricing versions')).toBeInTheDocument()
  })

  it('expands to show historic versions when clicked', async () => {
    const user = userEvent.setup()
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion]}
      />
    )

    // Initially collapsed
    expect(screen.queryByText(/until/)).not.toBeInTheDocument()

    // Click to expand
    await user.click(screen.getByText('1 historic pricing version'))

    // Should show the historic version
    expect(screen.getByText(/until/)).toBeInTheDocument()
  })

  it('displays formatted prices correctly', async () => {
    const user = userEvent.setup()
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion]}
      />
    )

    await user.click(screen.getByText('1 historic pricing version'))

    // Check for formatted prices (Dutch locale format)
    // The historic version has priceAdult: 45, priceChild: 35
    expect(screen.getByText(/€\s*45,00/)).toBeInTheDocument()
    expect(screen.getByText(/€\s*35,00/)).toBeInTheDocument()
  })

  it('displays validUntil date for historic versions', async () => {
    const user = userEvent.setup()
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion]}
      />
    )

    await user.click(screen.getByText('1 historic pricing version'))

    // Should show "until" badge with the date
    expect(screen.getByText(/until/)).toBeInTheDocument()
  })

  it('displays validFrom date for historic versions', async () => {
    const user = userEvent.setup()
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion]}
      />
    )

    await user.click(screen.getByText('1 historic pricing version'))

    // Should show "from" with the date
    expect(screen.getByText(/from/)).toBeInTheDocument()
  })

  it('collapses when clicked again', async () => {
    const user = userEvent.setup()
    render(
      <PricingHistoryCollapsible
        pricingHistory={[mockCurrentVersion, mockHistoricVersion]}
      />
    )

    // Expand
    await user.click(screen.getByText('1 historic pricing version'))
    expect(screen.getByText(/until/)).toBeInTheDocument()

    // Collapse
    await user.click(screen.getByText('1 historic pricing version'))

    // Content should be hidden (Radix Collapsible removes from DOM when closed)
    expect(screen.queryByText(/until/)).not.toBeInTheDocument()
  })
})
