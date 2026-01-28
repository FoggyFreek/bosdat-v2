import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import axios from 'axios'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'

describe('API Configuration', () => {
  it('creates axios instance with correct baseURL', () => {
    // The baseURL is set from import.meta.env.VITE_API_URL at module load time
    // In tests, this defaults to empty string, so baseURL is '/api'
    expect(api.defaults.baseURL).toMatch(/\/api$/)
  })

  it('sets Content-Type header to application/json', () => {
    expect(api.defaults.headers['Content-Type']).toBe('application/json')
  })
})

describe('Request Interceptor', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
  })

  it('adds Authorization header when token exists', async () => {
    localStorage.setItem('token', 'test-token')

    mock.onGet('/test').reply((config) => {
      expect(config.headers?.Authorization).toBe('Bearer test-token')
      return [200, {}]
    })

    await api.get('/test')
  })

  it('does not add Authorization header when token is missing', async () => {
    mock.onGet('/test').reply((config) => {
      expect(config.headers?.Authorization).toBeUndefined()
      return [200, {}]
    })

    await api.get('/test')
  })

  it('handles request interceptor error', async () => {
    // This tests the error path in the request interceptor
    mock.onGet('/test').networkError()

    await expect(api.get('/test')).rejects.toThrow()
  })
})

describe('Response Interceptor - Token Refresh', () => {
  let mock: MockAdapter
  let axiosMock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    axiosMock = new MockAdapter(axios)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
    axiosMock.restore()
  })

  it('refreshes token on 401 error and retries request', async () => {
    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    let callCount = 0
    mock.onGet('/protected').reply(() => {
      callCount++
      if (callCount === 1) {
        // First request fails with 401
        return [401]
      }
      // After refresh, retry succeeds
      return [200, { data: 'success' }]
    })

    // Refresh endpoint returns new tokens (using base axios instance)
    axiosMock.onPost('/api/auth/refresh').reply(200, {
      token: 'new-token',
      refreshToken: 'new-refresh-token',
    })

    const response = await api.get('/protected')

    expect(response.data).toEqual({ data: 'success' })
    expect(localStorage.getItem('token')).toBe('new-token')
    expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token')
    expect(callCount).toBe(2) // Verify it was called twice (original + retry)
  })

  it('does not refresh when no refresh token exists', async () => {
    localStorage.setItem('token', 'old-token')
    // No refresh token

    mock.onGet('/protected').reply(401)

    await expect(api.get('/protected')).rejects.toThrow()

    // Should not call refresh endpoint
    expect(axiosMock.history.post.length).toBe(0)
  })

  it('redirects to login when token refresh fails', async () => {
    // Mock window.location.href setter
    const mockHref = vi.fn()
    Object.defineProperty(window, 'location', {
      value: { href: '' },
      writable: true,
    })
    Object.defineProperty(window.location, 'href', {
      set: mockHref,
      get: () => '/login',
    })

    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onGet('/protected').reply(401)
    axiosMock.onPost('/api/auth/refresh').reply(401)

    await expect(api.get('/protected')).rejects.toThrow()

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(mockHref).toHaveBeenCalledWith('/login')
  })

  it('does not retry request with X-Retry header already set', async () => {
    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    let requestCount = 0
    mock.onGet('/protected').reply(() => {
      requestCount++
      return [401]
    })

    await expect(
      api.get('/protected', {
        headers: { 'X-Retry': 'true' },
      })
    ).rejects.toThrow()

    // Should only make one request, not retry
    expect(requestCount).toBe(1)
  })

  it('does not refresh on non-401 errors', async () => {
    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onGet('/error').reply(500)

    await expect(api.get('/error')).rejects.toThrow()

    // Should not call refresh endpoint
    expect(axiosMock.history.post.length).toBe(0)
  })

  it('passes through successful responses', async () => {
    mock.onGet('/success').reply(200, { message: 'success' })

    const response = await api.get('/success')

    expect(response.data).toEqual({ message: 'success' })
  })
})
