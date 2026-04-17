export interface AccessoryItem {
  id: string;
  name: string;
  thumbnail: string;
}

export interface GarmentItem {
  id: string;
  name: string;
  category: string;
  thumbnail: string;
}

export interface GarmentCategory {
  key: string;
  label: string;
}

export const GARMENT_CATEGORIES: GarmentCategory[] = [
  { key: 'all', label: 'All' },
  { key: 'bestseller', label: 'Bestseller' },
  { key: 'truyen-thong', label: 'Áo dài truyền thống' },
  { key: 'lua-tron', label: 'Áo dài lụa trơn' },
  { key: 'theu-hoa', label: 'Áo dài thêu hoa' },
  { key: 'cach-tan', label: 'Áo dài cách tân' },
];

export const ACCESSORIES: AccessoryItem[] = [
  { id: 'tram', name: 'Trâm cài tóc', thumbnail: '/assets/ai-tryon/products/phukien-1.png' },
  { id: 'quat', name: 'Quạt cầm tay', thumbnail: '/assets/ai-tryon/products/phukien-2.png' },
  { id: 'tui', name: 'Túi xách', thumbnail: '/assets/ai-tryon/products/phukien-3.png' },
];

export const GARMENTS: GarmentItem[] = [
  // Truyền thống
  { id: 'tt-1', name: 'Áo dài truyền thống đỏ', category: 'truyen-thong', thumbnail: '/assets/ai-tryon/products/aodai-tt-1.png' },
  { id: 'tt-2', name: 'Áo dài truyền thống xanh', category: 'truyen-thong', thumbnail: '/assets/ai-tryon/products/aodai-tt-2.png' },
  // Lụa trơn
  { id: 'lt-1', name: 'Áo dài lụa trơn trắng', category: 'lua-tron', thumbnail: '/assets/ai-tryon/products/aodai-lt-1.png' },
  { id: 'lt-2', name: 'Áo dài lụa trơn hồng', category: 'lua-tron', thumbnail: '/assets/ai-tryon/products/aodai-lt-2.png' },
  // Thêu hoa
  { id: 'th-1', name: 'Áo dài thêu hoa pastel', category: 'theu-hoa', thumbnail: '/assets/ai-tryon/products/aodai-th-1.png' },
  { id: 'th-2', name: 'Áo dài thêu hoa đỏ', category: 'theu-hoa', thumbnail: '/assets/ai-tryon/products/aodai-th-2.png' },
  // Cách tân
  { id: 'ct-1', name: 'Áo dài cách tân hiện đại', category: 'cach-tan', thumbnail: '/assets/ai-tryon/products/aodai-ct-1.png' },
  { id: 'ct-2', name: 'Áo dài cách tân cách điệu', category: 'cach-tan', thumbnail: '/assets/ai-tryon/products/aodai-ct-2.png' },
  { id: 'ct-3', name: 'Áo dài cách tân trẻ trung', category: 'cach-tan', thumbnail: '/assets/ai-tryon/products/aodai-ct-3.png' },
];

export const BESTSELLER_IDS = ['tt-1', 'th-2', 'ct-1', 'lt-2'];
