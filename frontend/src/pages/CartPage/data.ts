export interface CartItemData {
  id: string;
  name: string;
  variant: string;
  originalPrice: string;
  price: string;
  quantity: number;
  image: string;
}

export const cartItems: CartItemData[] = [
  {
    id: '1',
    name: 'Áo Dài Tơ Tằm Truyền Thống',
    variant: 'Vải lụa',
    originalPrice: '0 đ',
    price: '0 đ',
    quantity: 0,
    image: '/assets/products/product-truyen-thong-1.png',
  },
];
