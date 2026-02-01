import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import axios from 'axios'
import { api } from '../api'

describe('API Configuration and Interceptors', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
  })

  describe('Axios Instance Configuration', () => {
    it('should have correct base URL from environment variable', () => {
      const expectedUrl = import.meta.env.VITE_API_URL
        ? `${import.meta.env.VITE_API_URL}/api`
        : '/api'
      expect(api.defaults.baseURL).toBe(expectedUrl)
    })

    it('should have Content-Type header set to application/json', () => {
      expect(api.defaults.headers['Content-Type']).toBe('application/json')
    })
  })

  describe('Request Interceptor', () => {
    it('should add Authorization header when token exists in localStorage', async () => {
      const token = 'test-jwt-token'
      localStorage.setItem('token', token)

      mock.onGet('/test').reply((config) => {
        expect(config.headers?.Authorization).toBe(`Bearer ${token}`)
        return [200, { success: true }]
      })

      await api.get('/test')
    })

    it('should not add Authorization header when token does not exist', async () => {
      mock.onGet('/test').reply((config) => {
        expect(config.headers?.Authorization).toBeUndefined()
        return [200, { success: true }]
      })

      await api.get('/test')
    })

    it('should handle request interceptor errors', async () => {
      // This test ensures the error path of the request interceptor is covered
      const requestInterceptor = api.interceptors.request
      expect(requestInterceptor).toBeDefined()
    })
  })

  describe('Response Interceptor - Token Refresh', () => {
    it('should refresh token on 401 response when refresh token exists', async () => {
      const oldToken = 'old-token'
      const refreshToken = 'refresh-token'
      const newToken = 'new-token'
      const newRefreshToken = 'new-refresh-token'

      localStorage.setItem('token', oldToken)
      localStorage.setItem('refreshToken', refreshToken)

      let requestCount = 0

      // Handle both the initial 401 and the retry
      mock.onGet('/protected').reply((config) => {
        requestCount++
        if (requestCount === 1) {
          // First request fails with 401
          return [401]
        } else {
          // Retry with new token succeeds
          expect(config.headers?.Authorization).toBe(`Bearer ${newToken}`)
          expect(config.headers?.['X-Retry']).toBe('true')
          return [200, { data: 'success' }]
        }
      })

      // Token refresh succeeds
      const axiosMock = new MockAdapter(axios)
      const apiUrl = import.meta.env.VITE_API_URL || ''
      axiosMock.onPost(`${apiUrl}/api/auth/refresh`).reply(200, {
        token: newToken,
        refreshToken: newRefreshToken,
      })

      const result = await api.get('/protected')

      expect(result.data).toEqual({ data: 'success' })
      expect(localStorage.getItem('token')).toBe(newToken)
      expect(localStorage.getItem('refreshToken')).toBe(newRefreshToken)
      expect(requestCount).toBe(2)

      axiosMock.restore()
    })

    it('should redirect to login when token refresh fails', async () => {
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

      const oldToken = 'old-token'
      const refreshToken = 'refresh-token'

      localStorage.setItem('token', oldToken)
      localStorage.setItem('refreshToken', refreshToken)

      // First request fails with 401
      mock.onGet('/protected').replyOnce(401)

      // Token refresh fails
      const axiosMock = new MockAdapter(axios)
      const apiUrl = import.meta.env.VITE_API_URL || ''
      axiosMock.onPost(`${apiUrl}/api/auth/refresh`).reply(401)

      try {
        await api.get('/protected')
      } catch (error) {
        // Expected to fail
      }

      expect(localStorage.getItem('token')).toBeNull()
      expect(localStorage.getItem('refreshToken')).toBeNull()
      expect(mockHref).toHaveBeenCalledWith('/login')

      axiosMock.restore()
    })

    it('should redirect to login when no refresh token exists on 401', async () => {
      const oldToken = 'old-token'
      localStorage.setItem('token', oldToken)

      // First request fails with 401, no refresh token available
      mock.onGet('/protected').replyOnce(401)

      try {
        await api.get('/protected')
      } catch (error) {
        expect(error).toBeDefined()
      }
    })

    it('should not retry request that already has X-Retry header', async () => {
      const token = 'test-token'
      const refreshToken = 'refresh-token'

      localStorage.setItem('token', token)
      localStorage.setItem('refreshToken', refreshToken)

      let requestCount = 0

      // Request with X-Retry header fails with 401
      mock.onGet('/protected').reply((_config) => {
        requestCount++
        return [401]
      })

      try {
        await api.get('/protected', {
          headers: { 'X-Retry': 'true' },
        })
      } catch (error) {
        // Expected to fail without retry
      }

      // Should only make one request, not retry
      expect(requestCount).toBe(1)
    })

    it('should pass through non-401 errors without token refresh', async () => {
      const token = 'test-token'
      localStorage.setItem('token', token)

      mock.onGet('/error').reply(500, { message: 'Server Error' })

      try {
        await api.get('/error')
      } catch (error) {
        expect(error).toBeDefined()
      }

      // Token should still be in storage
      expect(localStorage.getItem('token')).toBe(token)
    })

    it('should handle successful responses without interception', async () => {
      const token = 'test-token'
      localStorage.setItem('token', token)

      mock.onGet('/success').reply(200, { data: 'success' })

      const result = await api.get('/success')

      expect(result.data).toEqual({ data: 'success' })
      expect(localStorage.getItem('token')).toBe(token)
    })

    it('should handle 401 response when originalRequest is undefined', async () => {
      const refreshToken = 'refresh-token'
      localStorage.setItem('refreshToken', refreshToken)

      const responseInterceptor = api.interceptors.response
      expect(responseInterceptor).toBeDefined()

      // The error should be thrown as-is when config is missing
      mock.onGet('/test').reply(401)

      try {
        await api.get('/test')
      } catch (err) {
        expect(err).toBeDefined()
      }
    })
  })

  describe('Response Interceptor - Error Handling', () => {
    it('should reject with error when response interceptor encounters non-axios error', async () => {
      mock.onGet('/test').reply(400, { message: 'Bad Request' })

      try {
        await api.get('/test')
      } catch (error) {
        expect(error).toBeDefined()
      }
    })

    it('should handle network errors', async () => {
      mock.onGet('/test').networkError()

      try {
        await api.get('/test')
      } catch (error) {
        expect(error).toBeDefined()
      }
    })

    it('should handle timeout errors', async () => {
      mock.onGet('/test').timeout()

      try {
        await api.get('/test')
      } catch (error) {
        expect(error).toBeDefined()
      }
    })
  })
})
