export interface UserAddress {
  id: number;
  recipientName: string;
  recipientPhone: string;
  province: string;
  district: string;
  ward: string | null;
  addressLine: string;
  isDefault: boolean;
}

export interface CreateAddressPayload {
  recipientName: string;
  recipientPhone: string;
  province: string;
  district: string;
  ward?: string;
  addressLine: string;
  isDefault?: boolean;
}
