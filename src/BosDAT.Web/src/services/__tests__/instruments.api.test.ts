import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { instrumentsApi } from '@/features/instruments/api'

describe('instrumentsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches instruments', async () => {
    const instruments = [{ id: 1, name: 'Piano' }]
    mock.onGet('/instruments').reply(200, instruments)

    const result = await instrumentsApi.getAll()

    expect(result).toEqual(instruments)
  })

  it('getAll with activeOnly param', async () => {
    const instruments = [{ id: 1, name: 'Piano' }]
    mock.onGet('/instruments').reply((config) => {
      expect(config.params).toEqual({ activeOnly: true })
      return [200, instruments]
    })

    const result = await instrumentsApi.getAll({ activeOnly: true })

    expect(result).toEqual(instruments)
  })

  it('getById fetches single instrument', async () => {
    const instrument = { id: 1, name: 'Piano' }
    mock.onGet('/instruments/1').reply(200, instrument)

    const result = await instrumentsApi.getById(1)

    expect(result).toEqual(instrument)
  })

  it('create posts instrument data', async () => {
    const newInstrument = { name: 'Guitar' }
    const created = { id: 2, ...newInstrument }
    mock.onPost('/instruments').reply(200, created)

    const result = await instrumentsApi.create(newInstrument)

    expect(result).toEqual(created)
  })

  it('update puts instrument data', async () => {
    const updates = { name: 'Updated Piano' }
    const updated = { id: 1, ...updates }
    mock.onPut('/instruments/1').reply(200, updated)

    const result = await instrumentsApi.update(1, updates)

    expect(result).toEqual(updated)
  })
})
