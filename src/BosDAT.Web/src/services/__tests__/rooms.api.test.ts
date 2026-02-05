import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '../api'
import { roomsApi } from '@/features/rooms/api'

describe('roomsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches rooms', async () => {
    const rooms = [{ id: 1, name: 'Room A' }]
    mock.onGet('/rooms').reply(200, rooms)

    const result = await roomsApi.getAll()

    expect(result).toEqual(rooms)
  })

  it('getById fetches single room', async () => {
    const room = { id: 1, name: 'Room A' }
    mock.onGet('/rooms/1').reply(200, room)

    const result = await roomsApi.getById(1)

    expect(result).toEqual(room)
  })

  it('create posts room data', async () => {
    const newRoom = { name: 'Room B' }
    const created = { id: 2, ...newRoom }
    mock.onPost('/rooms').reply(200, created)

    const result = await roomsApi.create(newRoom)

    expect(result).toEqual(created)
  })

  it('update puts room data', async () => {
    const updates = { name: 'Updated Room' }
    const updated = { id: 1, ...updates }
    mock.onPut('/rooms/1').reply(200, updated)

    const result = await roomsApi.update(1, updates)

    expect(result).toEqual(updated)
  })

  it('delete removes room', async () => {
    mock.onDelete('/rooms/1').reply(204)

    await roomsApi.delete(1)

    expect(mock.history.delete.length).toBe(1)
  })

  it('archive archives room', async () => {
    const archived = { id: 1, name: 'Room A', isArchived: true }
    mock.onPut('/rooms/1/archive').reply(200, archived)

    const result = await roomsApi.archive(1)

    expect(result).toEqual(archived)
  })

  it('reactivate reactivates room', async () => {
    const reactivated = { id: 1, name: 'Room A', isArchived: false }
    mock.onPut('/rooms/1/reactivate').reply(200, reactivated)

    const result = await roomsApi.reactivate(1)

    expect(result).toEqual(reactivated)
  })
})
