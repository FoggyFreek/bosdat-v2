namespace BosDAT.Core.Enums;

public enum TransactionType
{
    InvoiceCharge= 0,
    Payment = 1,
    CreditCorrection = 2,
    DebitCorrection = 3,
    Reversal = 4,
    InvoiceCancellation = 5,
    InvoiceAdjustment = 6,
    CreditOffset = 7,
    CreditInvoice = 8,
    CreditApplied = 9
}
