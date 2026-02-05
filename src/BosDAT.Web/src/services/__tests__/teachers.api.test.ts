import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { teachersApi } from '@/features/teachers/api'

describe('teachersApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches teachers without params', async () => {
    const teachers = [{ id: '1', name: 'Teacher' }]
    mock.onGet('/teachers').reply(200, teachers)

    const result = await teachersApi.getAll()

    expect(result).toEqual(teachers)
  })

  it('getAll fetches teachers with params', async () => {
    const teachers = [{ id: '1', name: 'Teacher' }]
    mock.onGet('/teachers').reply((config) => {
      expect(config.params).toEqual({ activeOnly: true, instrumentId: 5, courseTypeId: 'ct-1' })
      return [200, teachers]
    })

    const result = await teachersApi.getAll({ activeOnly: true, instrumentId: 5, courseTypeId: 'ct-1' })

    expect(result).toEqual(teachers)
  })

  it('getById fetches single teacher', async () => {
    const teacher = { id: '1', name: 'Teacher' }
    mock.onGet('/teachers/1').reply(200, teacher)

    const result = await teachersApi.getById('1')

    expect(result).toEqual(teacher)
  })

  it('getWithCourses fetches teacher with courses', async () => {
    const data = { id: '1', name: 'Teacher', courses: [] }
    mock.onGet('/teachers/1/courses').reply(200, data)

    const result = await teachersApi.getWithCourses('1')

    expect(result).toEqual(data)
  })

  it('create posts teacher data', async () => {
    const newTeacher = { name: 'New Teacher' }
    const created = { id: '1', ...newTeacher }
    mock.onPost('/teachers').reply(200, created)

    const result = await teachersApi.create(newTeacher)

    expect(result).toEqual(created)
  })

  it('update puts teacher data', async () => {
    const updates = { name: 'Updated' }
    const updated = { id: '1', ...updates }
    mock.onPut('/teachers/1').reply(200, updated)

    const result = await teachersApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes teacher', async () => {
    mock.onDelete('/teachers/1').reply(204)

    await teachersApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('getAvailableCourseTypes with instrument IDs', async () => {
    const courseTypes = [{ id: 'ct-1', name: 'Piano' }]
    mock.onGet('/teachers/1/available-course-types').reply((config) => {
      expect(config.params).toEqual({ instrumentIds: '1,2,3' })
      return [200, courseTypes]
    })

    const result = await teachersApi.getAvailableCourseTypes('1', [1, 2, 3])

    expect(result).toEqual(courseTypes)
  })

  it('getAvailableCourseTypes without instrument IDs', async () => {
    const courseTypes = [{ id: 'ct-1', name: 'Piano' }]
    mock.onGet('/teachers/1/available-course-types').reply((config) => {
      expect(config.params).toEqual({})
      return [200, courseTypes]
    })

    const result = await teachersApi.getAvailableCourseTypes('1', [])

    expect(result).toEqual(courseTypes)
  })
})
