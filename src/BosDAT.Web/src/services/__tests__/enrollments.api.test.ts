import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { enrollmentsApi } from '@/features/enrollments/api'

describe('enrollmentsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches enrollments with params', async () => {
    const enrollments = [{ id: '1' }]
    mock.onGet('/enrollments').reply((config) => {
      expect(config.params).toEqual({ studentId: 's-1', courseId: 'c-1', status: 'Active' })
      return [200, enrollments]
    })

    const result = await enrollmentsApi.getAll({ studentId: 's-1', courseId: 'c-1', status: 'Active' })

    expect(result).toEqual(enrollments)
  })

  it('getById fetches single enrollment', async () => {
    const enrollment = { id: '1' }
    mock.onGet('/enrollments/1').reply(200, enrollment)

    const result = await enrollmentsApi.getById('1')

    expect(result).toEqual(enrollment)
  })

  it('getByStudent fetches student enrollments', async () => {
    const enrollments = [{ id: '1' }]
    mock.onGet('/enrollments/student/s-1').reply(200, enrollments)

    const result = await enrollmentsApi.getByStudent('s-1')

    expect(result).toEqual(enrollments)
  })

  it('create posts enrollment data', async () => {
    const newEnrollment = { studentId: 's-1', courseId: 'c-1', discountPercent: 10, notes: 'Test' }
    const created = { id: '1', ...newEnrollment }
    mock.onPost('/enrollments').reply(200, created)

    const result = await enrollmentsApi.create(newEnrollment)

    expect(result).toEqual(created)
  })

  it('update puts enrollment data', async () => {
    const updates = { discountPercent: 15, status: 'Active', notes: 'Updated' }
    const updated = { id: '1', ...updates }
    mock.onPut('/enrollments/1').reply(200, updated)

    const result = await enrollmentsApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes enrollment', async () => {
    mock.onDelete('/enrollments/1').reply(204)

    await enrollmentsApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('promoteFromTrail promotes enrollment', async () => {
    const promoted = { id: '1', status: 'Active' }
    mock.onPut('/enrollments/1/promote').reply(200, promoted)

    const result = await enrollmentsApi.promoteFromTrail('1')

    expect(result).toEqual(promoted)
  })

})
