export interface MockVariant {
  id: string
  sku: string
  variantName: string
  size: string
  color: string
  price: number
  salePrice: number | null
  stockQty: number
  isDefault: boolean
}

export interface MockImage {
  id: string
  url: string
  altText: string
  isPrimary: boolean
  sortOrder: number
}

export interface MockProduct {
  id: string
  name: string
  slug: string
  type: 'ao-dai' | 'phu-kien'
  categoryId: string
  categoryName: string
  shortDescription: string
  description: string
  material: string
  brand: string
  origin: string
  careInstruction: string
  status: 'active' | 'draft' | 'inactive'
  isFeatured: boolean
  sortOrder: number
  createdAt: string
  variants: MockVariant[]
  images: MockImage[]
}

export interface Category {
  id: string
  name: string
  slug: string
  children?: Category[]
}

export const categories: Category[] = [
  {
    id: 'cat-1', name: 'Áo dài', slug: 'ao-dai', children: [
      { id: 'cat-1-1', name: 'Áo dài truyền thống', slug: 'truyen-thong' },
      { id: 'cat-1-2', name: 'Áo dài cách tân', slug: 'cach-tan' },
      { id: 'cat-1-3', name: 'Áo dài lụa trơn', slug: 'lua-tron' },
      { id: 'cat-1-4', name: 'Áo dài thêu hoa', slug: 'theu-hoa' },
    ]
  },
  {
    id: 'cat-2', name: 'Phụ kiện', slug: 'phu-kien', children: [
      { id: 'cat-2-1', name: 'Trâm cài', slug: 'tram-cai' },
      { id: 'cat-2-2', name: 'Túi sách', slug: 'tui-sach' },
      { id: 'cat-2-3', name: 'Quạt', slug: 'quat' },
      { id: 'cat-2-4', name: 'Giày', slug: 'giay' },
    ]
  },
]

function img(seed: string, alt: string, primary = false): MockImage {
  return {
    id: `img-${seed}`,
    url: `https://picsum.photos/seed/${seed}/400/400`,
    altText: alt,
    isPrimary: primary,
    sortOrder: 0,
  }
}

export const initialProducts: MockProduct[] = [
  // --- Áo dài ---
  {
    id: 'ad-1', name: 'Áo dài truyền thống Nhã Uyên', slug: 'ao-dai-truyen-thong-nha-uyen',
    type: 'ao-dai', categoryId: 'cat-1-1', categoryName: 'Áo dài truyền thống',
    shortDescription: 'Áo dài truyền thống may đo tinh xảo, chất liệu lụa cao cấp.',
    description: 'Áo dài truyền thống Nhã Uyên được may đo thủ công bởi các nghệ nhân lành nghề. Chất liệu lụa tơ tằm tự nhiên, tạo cảm giác mềm mại, thoáng mát. Phù hợp cho các dịp lễ Tết, cưới hỏi và sự kiện quan trọng.',
    material: 'Lụa tơ tằm', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Giặt tay nhẹ nhàng, không vắt, phơi trong bóng râm.',
    status: 'active', isFeatured: true, sortOrder: 1,
    createdAt: '2025-12-01T00:00:00Z',
    variants: [
      { id: 'v-ad1-1', sku: 'ADTNY-S', variantName: 'Size S', size: 'S', color: 'Đỏ', price: 2500000, salePrice: null, stockQty: 15, isDefault: true },
      { id: 'v-ad1-2', sku: 'ADTNY-M', variantName: 'Size M', size: 'M', color: 'Đỏ', price: 2500000, salePrice: 2200000, stockQty: 20, isDefault: false },
      { id: 'v-ad1-3', sku: 'ADTNY-L', variantName: 'Size L', size: 'L', color: 'Đỏ', price: 2500000, salePrice: null, stockQty: 8, isDefault: false },
    ],
    images: [
      img('ad-truyen-thong-1', 'Áo dài truyền thống Nhã Uyên - mặt trước', true),
      img('ad-truyen-thong-2', 'Áo dài truyền thống Nhã Uyên - mặt sau', false),
    ],
  },
  {
    id: 'ad-2', name: 'Áo dài cách tân hoa sen', slug: 'ao-dai-cach-tan-hoa-sen',
    type: 'ao-dai', categoryId: 'cat-1-2', categoryName: 'Áo dài cách tân',
    shortDescription: 'Thiết kế cách tân trẻ trung, họa tiết hoa sen thêu tay.',
    description: 'Áo dài cách tân hoa sen kết hợp giữa nét truyền thống và hiện đại. Họa tiết hoa sen được thêu tay tỉ mỉ trên nền lụa trắng. Phù hợp cho các bạn trẻ yêu thích phong cách áo dài mới lạ.',
    material: 'Lụa pha cotton', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Giặt khô hoặc giặt tay, không dùng chất tẩy mạnh.',
    status: 'active', isFeatured: true, sortOrder: 2,
    createdAt: '2025-12-05T00:00:00Z',
    variants: [
      { id: 'v-ad2-1', sku: 'ADCHS-M', variantName: 'Size M', size: 'M', color: 'Trắng', price: 1800000, salePrice: 1500000, stockQty: 10, isDefault: true },
      { id: 'v-ad2-2', sku: 'ADCHS-L', variantName: 'Size L', size: 'L', color: 'Trắng', price: 1800000, salePrice: null, stockQty: 12, isDefault: false },
    ],
    images: [
      img('ad-cach-tan-sen-1', 'Áo dài cách tân hoa sen - mặt trước', true),
      img('ad-cach-tan-sen-2', 'Áo dài cách tân hoa sen - chi tiết thêu', false),
    ],
  },
  {
    id: 'ad-3', name: 'Áo dài lụa trơn vàng hoàng yến', slug: 'ao-dai-lua-tron-vang-hoang-yen',
    type: 'ao-dai', categoryId: 'cat-1-3', categoryName: 'Áo dài lụa trơn',
    shortDescription: 'Áo dài lụa trơn màu vàng hoàng yến sang trọng.',
    description: 'Áo dài lụa trơn màu vàng hoàng yến là lựa chọn hoàn hảo cho những buổi tiệc sang trọng. Chất lụa bóng mịn, form dáng suông nhẹ tôn lên vẻ đẹp thanh lịch.',
    material: 'Lụa satin', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Giặt khô, ủi ở nhiệt độ thấp.',
    status: 'active', isFeatured: false, sortOrder: 3,
    createdAt: '2025-12-10T00:00:00Z',
    variants: [
      { id: 'v-ad3-1', sku: 'ADLTV-M', variantName: 'Size M', size: 'M', color: 'Vàng', price: 2200000, salePrice: null, stockQty: 5, isDefault: true },
    ],
    images: [
      img('ad-lua-tron-vang-1', 'Áo dài lụa trơn vàng hoàng yến', true),
    ],
  },
  {
    id: 'ad-4', name: 'Áo dài thêu hoa mẫu đơn', slug: 'ao-dai-theu-hoa-mau-don',
    type: 'ao-dai', categoryId: 'cat-1-4', categoryName: 'Áo dài thêu hoa',
    shortDescription: 'Áo dài thêu hoa mẫu đơn cầu kỳ, dành cho cô dâu.',
    description: 'Áo dài thêu hoa mẫu đơn là tác phẩm nghệ thuật thêu tay trên nền lụa đỏ. Mỗi bông hoa được thêu tỉ mỉ với hàng ngàn mũi chỉ vàng. Dành riêng cho cô dâu trong ngày trọng đại.',
    material: 'Lụa tơ tằm cao cấp', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Chỉ giặt khô, bảo quản trong túi vải.',
    status: 'active', isFeatured: true, sortOrder: 4,
    createdAt: '2025-12-15T00:00:00Z',
    variants: [
      { id: 'v-ad4-1', sku: 'ADTHMD-S', variantName: 'Size S', size: 'S', color: 'Đỏ', price: 4500000, salePrice: null, stockQty: 3, isDefault: true },
      { id: 'v-ad4-2', sku: 'ADTHMD-M', variantName: 'Size M', size: 'M', color: 'Đỏ', price: 4500000, salePrice: 4000000, stockQty: 2, isDefault: false },
    ],
    images: [
      img('ad-theu-mau-don-1', 'Áo dài thêu hoa mẫu đơn - tổng thể', true),
      img('ad-theu-mau-don-2', 'Áo dài thêu hoa mẫu đơn - chi tiết thêu', false),
      img('ad-theu-mau-don-3', 'Áo dài thêu hoa mẫu đơn - mặt sau', false),
    ],
  },
  {
    id: 'ad-5', name: 'Áo dài cách tân cổ thuyền', slug: 'ao-dai-cach-tan-co-thuyen',
    type: 'ao-dai', categoryId: 'cat-1-2', categoryName: 'Áo dài cách tân',
    shortDescription: 'Áo dài cách tân cổ thuyền thanh lịch, phù hợp công sở.',
    description: 'Áo dài cách tân cổ thuyền thiết kế đơn giản mà tinh tế. Chất liệu lụa cotton thoáng mát, form dáng ôm nhẹ. Phù hợp mặc đi làm, đi sự kiện ban ngày.',
    material: 'Lụa cotton', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Giặt tay hoặc giặt máy chế độ nhẹ.',
    status: 'draft', isFeatured: false, sortOrder: 5,
    createdAt: '2026-01-20T00:00:00Z',
    variants: [
      { id: 'v-ad5-1', sku: 'ADCCT-M', variantName: 'Size M', size: 'M', color: 'Xanh navy', price: 1600000, salePrice: null, stockQty: 0, isDefault: true },
    ],
    images: [
      img('ad-co-thuyen-1', 'Áo dài cách tân cổ thuyền', true),
    ],
  },
  {
    id: 'ad-6', name: 'Áo dài lụa trơn đỏ burgundy', slug: 'ao-dai-lua-tron-do-burgundy',
    type: 'ao-dai', categoryId: 'cat-1-3', categoryName: 'Áo dài lụa trơn',
    shortDescription: 'Áo dài lụa trơn đỏ burgundy, đậm chất quý phái.',
    description: 'Áo dài lụa trơn màu đỏ burgundy là biểu tượng của sự quý phái. Chất lụa dày dặn, giữ form tốt. Phù hợp cho các sự kiện trang trọng như tiệc tất niên, lễ kỷ niệm.',
    material: 'Lụa tơ tằm dày', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Giặt khô, ủi mặt trái ở nhiệt độ thấp.',
    status: 'inactive', isFeatured: false, sortOrder: 6,
    createdAt: '2025-11-15T00:00:00Z',
    variants: [
      { id: 'v-ad6-1', sku: 'ADLTDB-M', variantName: 'Size M', size: 'M', color: 'Đỏ burgundy', price: 2800000, salePrice: null, stockQty: 0, isDefault: true },
    ],
    images: [
      img('ad-burgundy-1', 'Áo dài lụa trơn đỏ burgundy', true),
    ],
  },

  // --- Phụ kiện ---
  {
    id: 'pk-1', name: 'Trâm cài ngọc trai', slug: 'tram-cai-ngoc-trai',
    type: 'phu-kien', categoryId: 'cat-2-1', categoryName: 'Trâm cài',
    shortDescription: 'Trâm cài tóc đính ngọc trai tự nhiên, mạ vàng 18K.',
    description: 'Trâm cài tóc đính ngọc trai nước ngọt tự nhiên, được chế tác thủ công với lớp mạ vàng 18K. Phụ kiện hoàn hảo cho bộ áo dài truyền thống.',
    material: 'Ngọc trai, hợp kim mạ vàng 18K', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Tránh tiếp xúc với nước và hóa chất. Lau bằng vải mềm.',
    status: 'active', isFeatured: true, sortOrder: 1,
    createdAt: '2025-12-20T00:00:00Z',
    variants: [
      { id: 'v-pk1-1', sku: 'TCNT-V', variantName: 'Vàng', size: 'Một kích thước', color: 'Vàng', price: 850000, salePrice: null, stockQty: 30, isDefault: true },
    ],
    images: [
      img('pk-tram-cai-ngoc-trai-1', 'Trâm cài ngọc trai', true),
      img('pk-tram-cai-ngoc-trai-2', 'Trâm cài ngọc trai - chi tiết', false),
    ],
  },
  {
    id: 'pk-2', name: 'Túi cầm tay thêu hoa sen', slug: 'tui-cam-tay-theu-hoa-sen',
    type: 'phu-kien', categoryId: 'cat-2-2', categoryName: 'Túi sách',
    shortDescription: 'Túi cầm tay lụa thêu hoa sen, dây xích vàng.',
    description: 'Túi cầm tay bằng lụa cao cấp, thêu tay họa tiết hoa sen tinh xảo. Dây xích mạ vàng sang trọng. Kích thước vừa phải, đựng được điện thoại và đồ trang điểm cơ bản.',
    material: 'Lụa, hợp kim mạ vàng', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Tránh ẩm, bảo quản trong túi vải khi không sử dụng.',
    status: 'active', isFeatured: true, sortOrder: 2,
    createdAt: '2025-12-22T00:00:00Z',
    variants: [
      { id: 'v-pk2-1', sku: 'TCTS-Đ', variantName: 'Đen', size: 'Một kích thước', color: 'Đen', price: 650000, salePrice: 520000, stockQty: 20, isDefault: true },
      { id: 'v-pk2-2', sku: 'TCTS-Đo', variantName: 'Đỏ', size: 'Một kích thước', color: 'Đỏ', price: 650000, salePrice: null, stockQty: 15, isDefault: false },
    ],
    images: [
      img('pk-tui-theu-sen-1', 'Túi cầm tay thêu hoa sen', true),
    ],
  },
  {
    id: 'pk-3', name: 'Quạt lụa vẽ tay', slug: 'quat-lua-ve-tay',
    type: 'phu-kien', categoryId: 'cat-2-3', categoryName: 'Quạt',
    shortDescription: 'Quạt lụa vẽ tay họa tiết phong cảnh Việt Nam.',
    description: 'Quạt lụa được vẽ tay bởi các họa sĩ với họa tiết phong cảnh làng quê Việt Nam. Khung tre tự nhiên, lụa trắng cao cấp. Phụ kiện không thể thiếu cho bộ áo dài.',
    material: 'Tre, lụa', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Tránh nước, gấp nhẹ nhàng khi không sử dụng.',
    status: 'active', isFeatured: false, sortOrder: 3,
    createdAt: '2026-01-05T00:00:00Z',
    variants: [
      { id: 'v-pk3-1', sku: 'QLVT-T', variantName: 'Tiêu chuẩn', size: 'Một kích thước', color: 'Trắng', price: 350000, salePrice: null, stockQty: 50, isDefault: true },
    ],
    images: [
      img('pk-quat-lua-1', 'Quạt lụa vẽ tay - mở', true),
      img('pk-quat-lua-2', 'Quạt lụa vẽ tay - gấp', false),
    ],
  },
  {
    id: 'pk-4', name: 'Giày thêu hoa sen', slug: 'giay-theu-hoa-sen',
    type: 'phu-kien', categoryId: 'cat-2-4', categoryName: 'Giày',
    shortDescription: 'Giày mũi nhọn thêu hoa sen, đế thấp êm chân.',
    description: 'Giày mũi nhọn được thêu tay họa tiết hoa sen trên nền lụa. Đế thấp 3cm, lót êm chân, phù hợp đi cả ngày. Kết hợp hoàn hảo với áo dài.',
    material: 'Lụa, da đế', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Tránh nước, vệ sinh bằng khăn ẩm.',
    status: 'active', isFeatured: false, sortOrder: 4,
    createdAt: '2026-01-10T00:00:00Z',
    variants: [
      { id: 'v-pk4-1', sku: 'GTHS-36', variantName: 'Size 36', size: '36', color: 'Đen', price: 550000, salePrice: null, stockQty: 12, isDefault: true },
      { id: 'v-pk4-2', sku: 'GTHS-37', variantName: 'Size 37', size: '37', color: 'Đen', price: 550000, salePrice: null, stockQty: 18, isDefault: false },
      { id: 'v-pk4-3', sku: 'GTHS-38', variantName: 'Size 38', size: '38', color: 'Đen', price: 550000, salePrice: 480000, stockQty: 10, isDefault: false },
    ],
    images: [
      img('pk-giay-theu-1', 'Giày thêu hoa sen', true),
    ],
  },
  {
    id: 'pk-5', name: 'Trâm cài hoa mai', slug: 'tram-cai-hoa-mai',
    type: 'phu-kien', categoryId: 'cat-2-1', categoryName: 'Trâm cài',
    shortDescription: 'Trâm cài hoa mai 5 cánh, mạ vàng 24K.',
    description: 'Trâm cài hoa mai vàng rực rỡ, biểu tượng của mùa xuân và may mắn. Được chế tác thủ công với 5 cánh hoa mai mềm mại. Phù hợp mặc áo dài dịp Tết.',
    material: 'Hợp kim mạ vàng 24K', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Tránh ẩm, lau bằng vải mềm sau khi sử dụng.',
    status: 'draft', isFeatured: false, sortOrder: 5,
    createdAt: '2026-02-01T00:00:00Z',
    variants: [
      { id: 'v-pk5-1', sku: 'TCHM-V', variantName: 'Vàng', size: 'Một kích thước', color: 'Vàng', price: 720000, salePrice: null, stockQty: 0, isDefault: true },
    ],
    images: [
      img('pk-tram-cai-mai-1', 'Trâm cài hoa mai', true),
    ],
  },
  {
    id: 'pk-6', name: 'Túi đeo chéo mini lụa', slug: 'tui-deo-cheo-mini-lua',
    type: 'phu-kien', categoryId: 'cat-2-2', categoryName: 'Túi sách',
    shortDescription: 'Túi đeo chéo mini bằng lụa, dây xích mạ vàng.',
    description: 'Túi đeo chéo mini bằng lụa cao cấp, thiết kế nhỏ gọn cho các buổi dạo phố. Dây xích mạ vàng có thể tháo rời. Đựng vừa điện thoại và ví nhỏ.',
    material: 'Lụa, hợp kim mạ vàng', brand: 'Nhã Uyên', origin: 'Việt Nam',
    careInstruction: 'Không giặt, lau bằng khăn ẩm. Bảo quản nơi khô ráo.',
    status: 'inactive', isFeatured: false, sortOrder: 6,
    createdAt: '2025-10-01T00:00:00Z',
    variants: [
      { id: 'v-pk6-1', sku: 'TDCML-H', variantName: 'Hồng', size: 'Một kích thước', color: 'Hồng pastel', price: 480000, salePrice: null, stockQty: 0, isDefault: true },
    ],
    images: [
      img('pk-tui-mini-lua-1', 'Túi đeo chéo mini lụa', true),
    ],
  },
]
