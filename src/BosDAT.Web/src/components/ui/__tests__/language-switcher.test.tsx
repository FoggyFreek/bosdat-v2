import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { LanguageSwitcher } from '../language-switcher'
import * as i18next from 'react-i18next'

describe('LanguageSwitcher', () => {
  let mockChangeLanguage: ReturnType<typeof vi.fn>

  beforeEach(() => {
    // Reset the mock before each test
    mockChangeLanguage = vi.fn()

    // Spy on useTranslation to return a mock with our spy function
    vi.spyOn(i18next, 'useTranslation').mockReturnValue({
      t: (key: string) => key,
      i18n: {
        language: 'nl',
        changeLanguage: mockChangeLanguage,
      } as any,
      ready: true,
    })
  })

  it('renders language flag buttons', () => {
    render(<LanguageSwitcher />)
    const buttons = screen.getAllByRole('button')
    expect(buttons).toHaveLength(2)
    expect(buttons[0]).toHaveAttribute('title', 'Nederlands')
    expect(buttons[1]).toHaveAttribute('title', 'English')
  })

  it('shows Dutch and English flags as SVG elements', () => {
    const { container } = render(<LanguageSwitcher />)
    const svgs = container.querySelectorAll('svg')
    expect(svgs).toHaveLength(2)
  })

  it('calls changeLanguage when clicking a language button', () => {
    render(<LanguageSwitcher />)
    const buttons = screen.getAllByRole('button')

    fireEvent.click(buttons[1]) // Click English button
    expect(mockChangeLanguage).toHaveBeenCalledWith('en')

    fireEvent.click(buttons[0]) // Click Dutch button
    expect(mockChangeLanguage).toHaveBeenCalledWith('nl')
  })
})
