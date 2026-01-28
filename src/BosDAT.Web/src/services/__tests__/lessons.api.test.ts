import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api, lessonsApi } from '../api'

describe('lessonsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches lessons with params', async () => {
    const lessons = [{ id: '1' }]
    const params = {
      startDate: '2024-01-01',
      endDate: '2024-01-31',
      teacherId: 't-1',
      studentId: 's-1',
      roomId: 1,
      status: 'Scheduled',
    }
    mock.onGet('/lessons').reply((config) => {
      expect(config.params).toEqual(params)
      return [200, lessons]
    })

    const result = await lessonsApi.getAll(params)

    expect(result).toEqual(lessons)
  })

  it('getById fetches single lesson', async () => {
    const lesson = { id: '1' }
    mock.onGet('/lessons/1').reply(200, lesson)

    const result = await lessonsApi.getById('1')

    expect(result).toEqual(lesson)
  })

  it('getByStudent fetches student lessons', async () => {
    const lessons = [{ id: '1' }]
    mock.onGet('/lessons/student/s-1').reply(200, lessons)

    const result = await lessonsApi.getByStudent('s-1')

    expect(result).toEqual(lessons)
  })

  it('create posts lesson data', async () => {
    const newLesson = { date: '2024-01-01', time: '10:00' }
    const created = { id: '1', ...newLesson }
    mock.onPost('/lessons').reply(200, created)

    const result = await lessonsApi.create(newLesson)

    expect(result).toEqual(created)
  })

  it('update puts lesson data', async () => {
    const updates = { time: '11:00' }
    const updated = { id: '1', ...updates }
    mock.onPut('/lessons/1').reply(200, updated)

    const result = await lessonsApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('updateStatus updates lesson status', async () => {
    const statusUpdate = { status: 'Cancelled', cancellationReason: 'Sick' }
    const updated = { id: '1', ...statusUpdate }
    mock.onPut('/lessons/1/status').reply(200, updated)

    const result = await lessonsApi.updateStatus('1', statusUpdate)

    expect(result).toEqual(updated)
  })

  it('delete removes lesson', async () => {
    mock.onDelete('/lessons/1').reply(204)

    await lessonsApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('generate generates lessons', async () => {
    const generateData = { courseId: 'c-1', startDate: '2024-01-01', endDate: '2024-01-31', skipHolidays: true }
    const generated = { count: 4 }
    mock.onPost('/lessons/generate').reply(200, generated)

    const result = await lessonsApi.generate(generateData)

    expect(result).toEqual(generated)
  })

  it('generateBulk generates bulk lessons', async () => {
    const bulkData = { startDate: '2024-01-01', endDate: '2024-01-31', skipHolidays: true }
    const generated = { count: 20 }
    mock.onPost('/lessons/generate-bulk').reply(200, generated)

    const result = await lessonsApi.generateBulk(bulkData)

    expect(result).toEqual(generated)
  })
})
