import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { settingsApi } from '@/features/settings/api'

describe('settingsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches settings', async () => {
    const settings = [{ key: 'theme', value: 'dark' }]
    mock.onGet('/settings').reply(200, settings)

    const result = await settingsApi.getAll()

    expect(result).toEqual(settings)
  })

  it('getByKey fetches single setting', async () => {
    const setting = { key: 'theme', value: 'dark' }
    mock.onGet('/settings/theme').reply(200, setting)

    const result = await settingsApi.getByKey('theme')

    expect(result).toEqual(setting)
  })

  it('update updates setting value', async () => {
    const updated = { key: 'theme', value: 'light' }
    mock.onPut('/settings/theme').reply(200, updated)

    const result = await settingsApi.update('theme', 'light')

    expect(result).toEqual(updated)
    expect(mock.history.put[0].data).toBe(JSON.stringify({ value: 'light' }))
  })
})
