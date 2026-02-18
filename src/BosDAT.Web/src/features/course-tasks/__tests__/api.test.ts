import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '@/services/api'
import { courseTasksApi } from '../api'

describe('courseTasksApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getByCourse fetches tasks for a course', async () => {
    const tasks = [{ id: 'task-1', courseId: 'course-1', title: 'Practice scales' }]
    mock.onGet('/courses/course-1/tasks').reply(200, tasks)

    const result = await courseTasksApi.getByCourse('course-1')

    expect(result).toEqual(tasks)
  })

  it('create posts a new task for a course', async () => {
    const newTask = { title: 'Learn chord progressions' }
    const created = { id: 'task-2', courseId: 'course-1', title: 'Learn chord progressions' }
    mock.onPost('/courses/course-1/tasks').reply(201, created)

    const result = await courseTasksApi.create('course-1', newTask)

    expect(result).toEqual(created)
    expect(mock.history.post[0].data).toBe(JSON.stringify(newTask))
  })

  it('delete removes a task', async () => {
    mock.onDelete('/courses/course-1/tasks/task-1').reply(204)

    await courseTasksApi.delete('course-1', 'task-1')

    expect(mock.history.delete).toHaveLength(1)
    expect(mock.history.delete[0].url).toBe('/courses/course-1/tasks/task-1')
  })
})
