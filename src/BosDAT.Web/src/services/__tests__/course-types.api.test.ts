import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api, courseTypesApi } from '../api'

describe('courseTypesApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches course types', async () => {
    const types = [{ id: '1', name: 'Individual' }]
    mock.onGet('/course-types').reply(200, types)

    const result = await courseTypesApi.getAll()

    expect(result).toEqual(types)
  })

  it('getAll with params', async () => {
    const types = [{ id: '1', name: 'Individual' }]
    mock.onGet('/course-types').reply((config) => {
      expect(config.params).toEqual({ activeOnly: true, instrumentId: 5 })
      return [200, types]
    })

    const result = await courseTypesApi.getAll({ activeOnly: true, instrumentId: 5 })

    expect(result).toEqual(types)
  })

  it('getById fetches single course type', async () => {
    const type = { id: '1', name: 'Individual' }
    mock.onGet('/course-types/1').reply(200, type)

    const result = await courseTypesApi.getById('1')

    expect(result).toEqual(type)
  })

  it('create posts course type data', async () => {
    const newType = { name: 'Group' }
    const created = { id: '2', ...newType }
    mock.onPost('/course-types').reply(200, created)

    const result = await courseTypesApi.create(newType)

    expect(result).toEqual(created)
  })

  it('update puts course type data', async () => {
    const updates = { name: 'Updated' }
    const updated = { id: '1', ...updates }
    mock.onPut('/course-types/1').reply(200, updated)

    const result = await courseTypesApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes course type', async () => {
    mock.onDelete('/course-types/1').reply(204)

    await courseTypesApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('reactivate reactivates course type', async () => {
    const reactivated = { id: '1', name: 'Individual', isActive: true }
    mock.onPut('/course-types/1/reactivate').reply(200, reactivated)

    const result = await courseTypesApi.reactivate('1')

    expect(result).toEqual(reactivated)
  })

  it('getTeacherCountForInstrument returns count', async () => {
    const count = { count: 5 }
    mock.onGet('/course-types/teachers-for-instrument/1').reply(200, count)

    const result = await courseTypesApi.getTeacherCountForInstrument(1)

    expect(result).toEqual(count)
  })

  it('getPricingHistory returns pricing history', async () => {
    const history = [{ validFrom: '2024-01-01', priceAdult: 50 }]
    mock.onGet('/course-types/1/pricing/history').reply(200, history)

    const result = await courseTypesApi.getPricingHistory('1')

    expect(result).toEqual(history)
  })

  it('checkPricingEditability returns editability status', async () => {
    const status = { canEdit: true, reason: '' }
    mock.onGet('/course-types/1/pricing/can-edit').reply(200, status)

    const result = await courseTypesApi.checkPricingEditability('1')

    expect(result).toEqual(status)
  })

  it('updatePricing updates pricing', async () => {
    const pricing = { priceAdult: 60, priceChild: 40 }
    const updated = { id: '1', ...pricing }
    mock.onPut('/course-types/1/pricing').reply(200, updated)

    const result = await courseTypesApi.updatePricing('1', pricing)

    expect(result).toEqual(updated)
  })

  it('createPricingVersion creates new pricing version', async () => {
    const versionData = { priceAdult: 60, priceChild: 40, validFrom: '2024-06-01' }
    const created = { id: 'v-1', ...versionData }
    mock.onPost('/course-types/1/pricing/versions').reply(200, created)

    const result = await courseTypesApi.createPricingVersion('1', versionData)

    expect(result).toEqual(created)
  })
})
