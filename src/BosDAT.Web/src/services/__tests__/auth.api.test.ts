import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api, authApi } from '../api'

describe('authApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
  })

  it('login returns auth response', async () => {
    const authResponse = {
      token: 'jwt-token',
      refreshToken: 'refresh-token',
      user: { id: '1', email: 'test@example.com' },
    }

    mock.onPost('/auth/login').reply(200, authResponse)

    const result = await authApi.login({
      email: 'test@example.com',
      password: 'password',
    })

    expect(result).toEqual(authResponse)
    expect(mock.history.post[0].data).toBe(JSON.stringify({
      email: 'test@example.com',
      password: 'password',
    }))
  })

  it('logout removes tokens and calls API with refresh token', async () => {
    localStorage.setItem('token', 'token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onPost('/auth/logout').reply(200)

    await authApi.logout()

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(mock.history.post.length).toBe(1)
    expect(mock.history.post[0].data).toBe(JSON.stringify({ refreshToken: 'refresh-token' }))
  })

  it('logout removes tokens even if API call fails', async () => {
    localStorage.setItem('token', 'token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onPost('/auth/logout').reply(500)

    await authApi.logout()

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
  })

  it('logout clears tokens when no refresh token exists', async () => {
    localStorage.setItem('token', 'token')

    await authApi.logout()

    expect(localStorage.getItem('token')).toBeNull()
    expect(mock.history.post.length).toBe(0)
  })

  it('getCurrentUser returns user data', async () => {
    const user = { id: '1', email: 'test@example.com', name: 'Test User' }
    mock.onGet('/auth/me').reply(200, user)

    const result = await authApi.getCurrentUser()

    expect(result).toEqual(user)
  })
})
