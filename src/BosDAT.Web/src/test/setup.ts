import '@testing-library/jest-dom'
import { cleanup } from '@testing-library/react'
import { afterEach, vi, beforeAll } from 'vitest'

// Suppress unhandled promise rejection warnings from react-query error tests
beforeAll(() => {
  const originalConsoleError = console.error
  vi.spyOn(console, 'error').mockImplementation((...args) => {
    if (
      typeof args[0] === 'string' &&
      args[0].includes('Warning: An update to')
    ) {
      return
    }
    originalConsoleError.call(console, ...args)
  })
})

// Cleanup after each test
afterEach(() => {
  cleanup()
})

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock ResizeObserver
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}))
