import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { holidaysApi } from '@/features/settings/api'

describe('holidaysApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches holidays', async () => {
    const holidays = [{ id: 1, name: 'Christmas' }]
    mock.onGet('/holidays').reply(200, holidays)

    const result = await holidaysApi.getAll()

    expect(result).toEqual(holidays)
  })

  it('create posts holiday data', async () => {
    const newHoliday = { name: 'New Year', startDate: '2024-01-01', endDate: '2024-01-01' }
    const created = { id: 1, ...newHoliday }
    mock.onPost('/holidays').reply(200, created)

    const result = await holidaysApi.create(newHoliday)

    expect(result).toEqual(created)
  })

  it('update puts holiday data', async () => {
    const updates = { name: 'Updated', startDate: '2024-01-01', endDate: '2024-01-02' }
    const updated = { id: 1, ...updates }
    mock.onPut('/holidays/1').reply(200, updated)

    const result = await holidaysApi.update(1, updates)

    expect(result).toEqual(updated)
  })

  it('delete removes holiday', async () => {
    mock.onDelete('/holidays/1').reply(204)

    await holidaysApi.delete(1)

    expect(mock.history.delete.length).toBe(1)
  })
})
