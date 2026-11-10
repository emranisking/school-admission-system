export type PaymentStatus = 'SUCCESS' | 'PENDING' | 'FAILED';

export interface PaymentDto {
  paymentStatus: PaymentStatus;
  transactionReference: string;
  amount: number;
  paidAt: string;
}
