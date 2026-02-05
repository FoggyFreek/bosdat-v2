import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { coursesApi } from '@/features/courses/api'

describe('coursesApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches courses with params', async () => {
    const courses = [{ id: '1', name: 'Piano 101' }]
    mock.onGet('/courses').reply((config) => {
      expect(config.params).toEqual({ status: 'Active', teacherId: 't-1', dayOfWeek: 1 })
      return [200, courses]
    })

    const result = await coursesApi.getAll({ status: 'Active', teacherId: 't-1', dayOfWeek: 1 })

    expect(result).toEqual(courses)
  })

  it('getAll fetches courses filtered by roomId', async () => {
    const courses = [{ id: '1', name: 'Piano 101' }]
    mock.onGet('/courses').reply((config) => {
      expect(config.params).toEqual({ roomId: 5 })
      return [200, courses]
    })

    const result = await coursesApi.getAll({ roomId: 5 })

    expect(result).toEqual(courses)
  })

  it('getById fetches single course', async () => {
    const course = { id: '1', name: 'Piano 101' }
    mock.onGet('/courses/1').reply(200, course)

    const result = await coursesApi.getById('1')

    expect(result).toEqual(course)
  })

  it('create posts course data', async () => {
    const newCourse = { name: 'Guitar 101' }
    const created = { id: '2', ...newCourse }
    mock.onPost('/courses').reply(200, created)

    const result = await coursesApi.create(newCourse)

    expect(result).toEqual(created)
  })

  it('update puts course data', async () => {
    const updates = { name: 'Updated Course' }
    const updated = { id: '1', ...updates }
    mock.onPut('/courses/1').reply(200, updated)

    const result = await coursesApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('enroll enrolls student in course', async () => {
    const enrollmentData = { studentId: 's-1', discountPercent: 10, notes: 'Test' }
    const enrollment = { id: 'e-1', ...enrollmentData }
    mock.onPost('/courses/c-1/enroll').reply(200, enrollment)

    const result = await coursesApi.enroll('c-1', enrollmentData)

    expect(result).toEqual(enrollment)
  })
})
