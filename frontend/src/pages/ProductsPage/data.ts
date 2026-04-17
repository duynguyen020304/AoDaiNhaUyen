export type Badge = 'HOT' | 'MỚI' | 'BÁN CHẠY';

export interface Product {
  id: string;
  name: string;
  image: string;
  badge?: Badge;
  reviews: number;
  price: string;
  originalPrice?: string;
}

export interface Category {
  id: string;
  name: string;
  products: Product[];
}

export const SIZES = ['S', 'M', 'L', 'XL'] as const;

export const CATEGORIES: Category[] = [
  {
    id: 'truyen-thong',
    name: 'Áo dài truyền thống',
    products: [
      { id: 'tt1', name: 'Áo Dài Cổ Truyền', image: '/assets/products/product-truyen-thong-1.png', badge: 'HOT', reviews: 48, price: '500.000đ' },
      { id: 'tt2', name: 'Áo Dài Tơ Tằm Truyền Thống', image: '/assets/products/product-truyen-thong-2.png', badge: 'MỚI', reviews: 35, price: '600.000đ' },
      { id: 'tt3', name: 'Áo Dài Truyền Thống Họa Tiết', image: '/assets/products/product-truyen-thong-3.png', reviews: 42, price: '500.000đ' },
      { id: 'tt4', name: 'Áo Dài Cổ Truyền Họa Tiết', image: '/assets/products/product-truyen-thong-4.png', reviews: 56, price: '400.000đ' },
      { id: 'tt5', name: 'Áo Dài Truyền Thống Sinh Viên', image: '/assets/products/product-truyen-thong-5.png', badge: 'BÁN CHẠY', reviews: 39, price: '300.000đ' },
      { id: 'tt6', name: 'Áo Dài Truyền Thống', image: '/assets/products/product-truyen-thong-6.png', reviews: 44, price: '300.000đ' },
    ],
  },
  {
    id: 'cach-tan',
    name: 'Áo dài cách tân',
    products: [
      { id: 'ct1', name: 'Áo Dài Hồng Cách Tân', image: '/assets/products/product-cach-tan-1.png', badge: 'HOT', reviews: 48, price: '500.000đ', originalPrice: '5.500.000đ' },
      { id: 'ct2', name: 'Áo Dài Cách Tân Xanh', image: '/assets/products/product-cach-tan-2.png', badge: 'MỚI', reviews: 35, price: '600.000đ' },
      { id: 'ct3', name: 'Áo Dài Cách Tân Họa Tiết', image: '/assets/products/product-cach-tan-3.png', reviews: 42, price: '500.000đ', originalPrice: '4.800.000đ' },
      { id: 'ct4', name: 'Áo Dài Cách Tân Họa Tiết', image: '/assets/products/product-cach-tan-4.png', reviews: 56, price: '600.000đ' },
      { id: 'ct5', name: 'Áo Dài Cách Tân', image: '/assets/products/product-cach-tan-5.png', badge: 'BÁN CHẠY', reviews: 39, price: '800.000đ' },
      { id: 'ct6', name: 'Áo Dài Cách Tân Đỏ', image: '/assets/products/product-cach-tan-6.png', reviews: 44, price: '600.000đ' },
    ],
  },
  {
    id: 'lua-tron',
    name: 'Áo dài lụa trơn',
    products: [
      { id: 'lt1', name: 'Áo Dài Đỏ Lụa Trơn', image: '/assets/products/product-lua-tron-1.png', badge: 'HOT', reviews: 48, price: '600.000đ' },
      { id: 'lt2', name: 'Áo Dài Tơ Tằm Lụa Trơn', image: '/assets/products/product-lua-tron-2.png', badge: 'MỚI', reviews: 35, price: '700.000đ' },
      { id: 'lt3', name: 'Áo Dài Lụa Trơn Xanh', image: '/assets/products/product-lua-tron-3.png', reviews: 42, price: '500.000đ' },
      { id: 'lt4', name: 'Áo Dài Lụa Trơn Tơ Tằm', image: '/assets/products/product-lua-tron-4.png', reviews: 56, price: '400.000đ' },
      { id: 'lt5', name: 'Áo Dài Lụa Trơn Trắng', image: '/assets/products/product-lua-tron-5.png', badge: 'BÁN CHẠY', reviews: 39, price: '300.000đ' },
      { id: 'lt6', name: 'Áo Dài Hồng Lụa Trơn', image: '/assets/products/product-lua-tron-6.png', reviews: 44, price: '400.000đ', originalPrice: '4.600.000đ' },
    ],
  },
  {
    id: 'theu-hoa',
    name: 'Áo dài thêu hoa',
    products: [
      { id: 'th1', name: 'Áo Dài Đỏ Thêu Hoa', image: '/assets/products/product-theu-hoa-1.png', badge: 'HOT', reviews: 48, price: '900.000đ', originalPrice: '5.500.000đ' },
      { id: 'th2', name: 'Áo Dài Thêu Hoa Tơ Tằm', image: '/assets/products/product-theu-hoa-2.png', badge: 'MỚI', reviews: 35, price: '1.000.000đ' },
      { id: 'th3', name: 'Áo Dài Thêu Hoa Họa Tiết', image: '/assets/products/product-theu-hoa-3.png', reviews: 42, price: '600.000đ', originalPrice: '4.800.000đ' },
      { id: 'th4', name: 'Áo Dài Thêu Hoa Trắng', image: '/assets/products/product-theu-hoa-4.png', reviews: 56, price: '600.000đ' },
      { id: 'th5', name: 'Áo Dài Xanh Thêu Sen', image: '/assets/products/product-theu-hoa-5.png', badge: 'BÁN CHẠY', reviews: 39, price: '400.000đ' },
      { id: 'th6', name: 'Áo Dài Hồng Thêu Hoa', image: '/assets/products/product-theu-hoa-6.png', reviews: 44, price: '500.000đ', originalPrice: '4.600.000đ' },
    ],
  },
];
