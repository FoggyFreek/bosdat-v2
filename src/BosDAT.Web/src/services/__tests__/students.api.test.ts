import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { studentsApi } from '@/features/students/api'

describe('studentsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches students without params', async () => {
    const students = [{ id: '1', name: 'John' }]
    mock.onGet('/students').reply(200, students)

    const result = await studentsApi.getAll()

    expect(result).toEqual(students)
  })

  it('getAll fetches students with params', async () => {
    const students = [{ id: '1', name: 'John' }]
    mock.onGet('/students').reply((config) => {
      expect(config.params).toEqual({ search: 'John', status: 'Active' })
      return [200, students]
    })

    const result = await studentsApi.getAll({ search: 'John', status: 'Active' })

    expect(result).toEqual(students)
  })

  it('getById fetches single student', async () => {
    const student = { id: '1', name: 'John' }
    mock.onGet('/students/1').reply(200, student)

    const result = await studentsApi.getById('1')

    expect(result).toEqual(student)
  })

  it('getWithEnrollments fetches student with enrollments', async () => {
    const data = { id: '1', name: 'John', enrollments: [] }
    mock.onGet('/students/1/enrollments').reply(200, data)

    const result = await studentsApi.getWithEnrollments('1')

    expect(result).toEqual(data)
  })

  it('create posts student data', async () => {
    const newStudent = { firstName: 'John', lastName: 'Doe' }
    const created = { id: '1', ...newStudent }
    mock.onPost('/students').reply(200, created)

    const result = await studentsApi.create(newStudent)

    expect(result).toEqual(created)
  })

  it('update puts student data', async () => {
    const updates = { firstName: 'Jane' }
    const updated = { id: '1', ...updates }
    mock.onPut('/students/1').reply(200, updated)

    const result = await studentsApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes student', async () => {
    mock.onDelete('/students/1').reply(204)

    await studentsApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
    expect(mock.history.delete[0].url).toBe('/students/1')
  })

  it('checkDuplicates returns duplicate check result', async () => {
    const checkData = {
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
    }
    const checkResult = { hasDuplicates: true, duplicates: [] }
    mock.onPost('/students/check-duplicates').reply(200, checkResult)

    const result = await studentsApi.checkDuplicates(checkData)

    expect(result).toEqual(checkResult)
  })

  it('getRegistrationFeeStatus returns registration fee status', async () => {
    const status = { hasPaid: true, amount: 50 }
    mock.onGet('/students/1/registration-fee').reply(200, status)

    const result = await studentsApi.getRegistrationFeeStatus('1')

    expect(result).toEqual(status)
  })

  it('hasActiveEnrollments returns boolean', async () => {
    mock.onGet('/students/1/has-active-enrollments').reply(200, true)

    const result = await studentsApi.hasActiveEnrollments('1')

    expect(result).toBe(true)
  })
})
