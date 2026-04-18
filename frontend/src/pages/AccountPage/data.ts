import type { UserProfile } from '../../types/user';
import type { UserAddress } from '../../types/address';
import type { UserOrder } from '../../types/order';

export const mockProfile: UserProfile = {
  id: 1,
  fullName: 'Nguyen Van A',
  email: 'nguyenvana@example.com',
  phone: '0901234567',
  dateOfBirth: '2000-04-20',
  gender: 'male',
  avatarUrl: null,
  address: '123 Nguyen Hue, Quan 1, TP. Ho Chi Minh',
};

export const mockAddresses: UserAddress[] = [
  {
    id: 1,
    recipientName: 'Nguyen Van A',
    recipientPhone: '0901234567',
    province: 'TP. Ho Chi Minh',
    district: 'Quan 1',
    ward: 'Ben Nghe',
    addressLine: '123 Nguyen Hue',
    isDefault: true,
  },
  {
    id: 2,
    recipientName: 'Nguyen Van A',
    recipientPhone: '0901234567',
    province: 'TP. Ho Chi Minh',
    district: 'Quan 3',
    ward: 'Phu Nhuan',
    addressLine: '456 Vo Van Tan',
    isDefault: false,
  },
];

export const mockOrders: UserOrder[] = [
  {
    id: 1,
    orderCode: 'AD-20260401-001',
    orderStatus: 'Da thanh toan',
    subtotal: 1500000,
    discountAmount: 0,
    shippingFee: 30000,
    totalAmount: 1530000,
    placedAt: '2026-04-01T10:30:00Z',
    paymentStatus: 'Da thanh toan',
    items: [
      {
        id: 1,
        productName: 'Ao Dai Lua Truyen Thong',
        size: 'M',
        color: 'Do',
        unitPrice: 1500000,
        quantity: 1,
        lineTotal: 1500000,
        imageUrl: null,
      },
    ],
  },
  {
    id: 2,
    orderCode: 'AD-20260410-002',
    orderStatus: 'Dang giao',
    subtotal: 2800000,
    discountAmount: 200000,
    shippingFee: 30000,
    totalAmount: 2630000,
    placedAt: '2026-04-10T14:20:00Z',
    paymentStatus: 'Da thanh toan',
    items: [
      {
        id: 2,
        productName: 'Ao Dai Cach Tan Hoa Tiet',
        size: 'L',
        color: 'Xanh',
        unitPrice: 1800000,
        quantity: 1,
        lineTotal: 1800000,
        imageUrl: null,
      },
      {
        id: 3,
        productName: 'Ao Dai Be Gai',
        size: 'S',
        color: 'Hong',
        unitPrice: 1000000,
        quantity: 1,
        lineTotal: 1000000,
        imageUrl: null,
      },
    ],
  },
];
