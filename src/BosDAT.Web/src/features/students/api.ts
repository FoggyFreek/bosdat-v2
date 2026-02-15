import { api } from '@/services/api'
import type {
  CheckDuplicatesDto,
  DuplicateCheckResult,
  RegistrationFeeStatus,
  StudentLedgerEntry,
  StudentLedgerSummary,
  CreateStudentLedgerEntry,
  DecoupleApplicationResult,
  Invoice,
  InvoiceListItem,
  InvoicePayment,
  GenerateInvoice,
  GenerateBatchInvoices,
  SchoolBillingInfo,
  InvoicePrintData,
  InvoiceStatus,
  StudentLedgerView,
  RecordPayment,
} from '@/features/students/types'

export const studentsApi = {
  getAll: async (params?: { search?: string; status?: string }) => {
    const response = await api.get('/students', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/students/${id}`)
    return response.data
  },

  getWithEnrollments: async (id: string) => {
    const response = await api.get(`/students/${id}/enrollments`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/students', data)
    return response.data
  },

  update: async (id: string, data: unknown) => {
    const response = await api.put(`/students/${id}`, data)
    return response.data
  },

  delete: async (id: string) => {
    await api.delete(`/students/${id}`)
  },

  checkDuplicates: async (data: CheckDuplicatesDto): Promise<DuplicateCheckResult> => {
    const response = await api.post<DuplicateCheckResult>('/students/check-duplicates', data)
    return response.data
  },

  getRegistrationFeeStatus: async (id: string): Promise<RegistrationFeeStatus> => {
    const response = await api.get<RegistrationFeeStatus>(`/students/${id}/registration-fee`)
    return response.data
  },

  hasActiveEnrollments: async (id: string): Promise<boolean> => {
    const response = await api.get<boolean>(`/students/${id}/has-active-enrollments`)
    return response.data
  },
}

export const studentLedgerApi = {
  getByStudent: async (studentId: string): Promise<StudentLedgerEntry[]> => {
    const response = await api.get<StudentLedgerEntry[]>(`/studentledger/student/${studentId}`)
    return response.data
  },

  getSummary: async (studentId: string): Promise<StudentLedgerSummary> => {
    const response = await api.get<StudentLedgerSummary>(`/studentledger/student/${studentId}/summary`)
    return response.data
  },

  getById: async (id: string): Promise<StudentLedgerEntry> => {
    const response = await api.get<StudentLedgerEntry>(`/studentledger/${id}`)
    return response.data
  },

  create: async (data: CreateStudentLedgerEntry): Promise<StudentLedgerEntry> => {
    const response = await api.post<StudentLedgerEntry>('/studentledger', data)
    return response.data
  },

  reverse: async (id: string, reason: string): Promise<StudentLedgerEntry> => {
    const response = await api.post<StudentLedgerEntry>(`/studentledger/${id}/reverse`, { reason })
    return response.data
  },

  getAvailableCredit: async (studentId: string): Promise<{ availableCredit: number }> => {
    const response = await api.get<{ availableCredit: number }>(`/studentledger/student/${studentId}/available-credit`)
    return response.data
  },

  decouple: async (applicationId: string, reason: string): Promise<DecoupleApplicationResult> => {
    const response = await api.post<DecoupleApplicationResult>(
      `/studentledger/applications/${applicationId}/decouple`,
      { reason },
    )
    return response.data
  },
}

export const invoicesApi = {
  getById: async (id: string): Promise<Invoice> => {
    const response = await api.get<Invoice>(`/invoices/${id}`)
    return response.data
  },

  getByNumber: async (invoiceNumber: string): Promise<Invoice> => {
    const response = await api.get<Invoice>(`/invoices/number/${invoiceNumber}`)
    return response.data
  },

  getByStudent: async (studentId: string): Promise<InvoiceListItem[]> => {
    const response = await api.get<InvoiceListItem[]>(`/invoices/student/${studentId}`)
    return response.data
  },

  getByStatus: async (status: InvoiceStatus): Promise<InvoiceListItem[]> => {
    const response = await api.get<InvoiceListItem[]>(`/invoices/status/${status}`)
    return response.data
  },

  generate: async (data: GenerateInvoice): Promise<Invoice> => {
    const response = await api.post<Invoice>('/invoices/generate', data)
    return response.data
  },

  generateBatch: async (data: GenerateBatchInvoices): Promise<Invoice[]> => {
    const response = await api.post<Invoice[]>('/invoices/generate-batch', data)
    return response.data
  },

  recalculate: async (id: string): Promise<Invoice> => {
    const response = await api.post<Invoice>(`/invoices/${id}/recalculate`)
    return response.data
  },

  applyCorrection: async (invoiceId: string, ledgerEntryId: string, amount: number): Promise<Invoice> => {
    const response = await api.post<Invoice>(`/invoices/${invoiceId}/apply-correction`, {
      ledgerEntryId,
      amount,
    })
    return response.data
  },

  getSchoolBillingInfo: async (): Promise<SchoolBillingInfo> => {
    const response = await api.get<SchoolBillingInfo>('/invoices/school-billing-info')
    return response.data
  },

  getPrintData: async (id: string): Promise<InvoicePrintData> => {
    const response = await api.get<InvoicePrintData>(`/invoices/${id}/print`)
    return response.data
  },

  recordPayment: async (invoiceId: string, data: RecordPayment): Promise<InvoicePayment> => {
    const response = await api.post<InvoicePayment>(`/invoices/${invoiceId}/payments`, data)
    return response.data
  },

  getPayments: async (invoiceId: string): Promise<InvoicePayment[]> => {
    const response = await api.get<InvoicePayment[]>(`/invoices/${invoiceId}/payments`)
    return response.data
  },
}

export const studentTransactionsApi = {
  getLedger: async (studentId: string): Promise<StudentLedgerView> => {
    const response = await api.get<StudentLedgerView>(`/students/${studentId}/transactions`)
    return response.data
  },

  getBalance: async (studentId: string): Promise<{ balance: number }> => {
    const response = await api.get<{ balance: number }>(`/students/${studentId}/transactions/balance`)
    return response.data
  },
}
