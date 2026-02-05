import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { calendarApi } from '@/features/schedule/api'

describe('calendarApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getWeek fetches week calendar', async () => {
    const data = [{ date: '2024-01-01', lessons: [] }]
    mock.onGet('/calendar/week').reply((config) => {
      expect(config.params).toEqual({ date: '2024-01-01', teacherId: 't-1', roomId: 1 })
      return [200, data]
    })

    const result = await calendarApi.getWeek({ date: '2024-01-01', teacherId: 't-1', roomId: 1 })

    expect(result).toEqual(data)
  })

  it('getDay fetches day calendar', async () => {
    const data = { date: '2024-01-01', lessons: [] }
    mock.onGet('/calendar/day').reply(200, data)

    const result = await calendarApi.getDay()

    expect(result).toEqual(data)
  })

  it('getMonth fetches month calendar', async () => {
    const data = [{ date: '2024-01-01', lessons: [] }]
    mock.onGet('/calendar/month').reply((config) => {
      expect(config.params).toEqual({ year: 2024, month: 1, teacherId: 't-1', roomId: 1 })
      return [200, data]
    })

    const result = await calendarApi.getMonth({ year: 2024, month: 1, teacherId: 't-1', roomId: 1 })

    expect(result).toEqual(data)
  })

  it('getTeacherSchedule fetches teacher schedule', async () => {
    const schedule = { lessons: [] }
    mock.onGet('/calendar/teacher/t-1').reply((config) => {
      expect(config.params).toEqual({ date: '2024-01-01' })
      return [200, schedule]
    })

    const result = await calendarApi.getTeacherSchedule('t-1', '2024-01-01')

    expect(result).toEqual(schedule)
  })

  it('getRoomSchedule fetches room schedule', async () => {
    const schedule = { lessons: [] }
    mock.onGet('/calendar/room/1').reply(200, schedule)

    const result = await calendarApi.getRoomSchedule(1)

    expect(result).toEqual(schedule)
  })

  it('checkAvailability checks availability', async () => {
    const params = {
      date: '2024-01-01',
      startTime: '10:00',
      endTime: '11:00',
      teacherId: 't-1',
      roomId: 1,
    }
    const availability = { isAvailable: true }
    mock.onGet('/calendar/availability').reply((config) => {
      expect(config.params).toEqual(params)
      return [200, availability]
    })

    const result = await calendarApi.checkAvailability(params)

    expect(result).toEqual(availability)
  })
})
