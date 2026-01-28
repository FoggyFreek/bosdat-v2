import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api, studentLedgerApi } from '../api'

describe('studentLedgerApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getByStudent fetches student ledger entries', async () => {
    const entries = [{ id: '1', amount: 50 }]
    mock.onGet('/studentledger/student/s-1').reply(200, entries)

    const result = await studentLedgerApi.getByStudent('s-1')

    expect(result).toEqual(entries)
  })

  it('getSummary fetches ledger summary', async () => {
    const summary = { totalDebit: 100, totalCredit: 50, balance: 50 }
    mock.onGet('/studentledger/student/s-1/summary').reply(200, summary)

    const result = await studentLedgerApi.getSummary('s-1')

    expect(result).toEqual(summary)
  })

  it('getById fetches single ledger entry', async () => {
    const entry = { id: '1', amount: 50 }
    mock.onGet('/studentledger/1').reply(200, entry)

    const result = await studentLedgerApi.getById('1')

    expect(result).toEqual(entry)
  })

  it('create posts ledger entry', async () => {
    const newEntry = {
      description: 'Test ledger entry',
      studentId: 's-1',
      amount: 50,
      entryType: 'Debit' as const,
    }
    const created = { id: '1', ...newEntry }
    mock.onPost('/studentledger').reply(200, created)

    const result = await studentLedgerApi.create(newEntry)

    expect(result).toEqual(created)
  })

  it('reverse reverses ledger entry', async () => {
    const reversed = { id: '2', amount: -50, originalEntryId: '1' }
    mock.onPost('/studentledger/1/reverse').reply(200, reversed)

    const result = await studentLedgerApi.reverse('1', 'Correction needed')

    expect(result).toEqual(reversed)
    expect(mock.history.post[0].data).toBe(JSON.stringify({ reason: 'Correction needed' }))
  })

  it('getAvailableCredit fetches available credit', async () => {
    const credit = { availableCredit: 100 }
    mock.onGet('/studentledger/student/s-1/available-credit').reply(200, credit)

    const result = await studentLedgerApi.getAvailableCredit('s-1')

    expect(result).toEqual(credit)
  })
})
