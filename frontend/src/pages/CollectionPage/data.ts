/* ── Image paths ────────────────────────────────────── */
export const IMG = {
  hero: '/assets/collection/hero-bg.jpg',
  separator: '/assets/collection/hero-separator.jpg',

  // Shared textures & decoration
  textureBg: '/assets/collection/texture-bg.jpg',
  patternDecor: '/assets/collection/pattern-decor.png',
  vectorDecor: '/assets/collection/vector-decor.svg',

  // bst2 – Truyền thống
  bst2Product: '/assets/collection/bst2-product.png',
  bst2Street: '/assets/collection/bst2-street.jpg',

  // bst3 – Lụa trơn
  bst3Product: '/assets/collection/bst3-product.png',
  bst3Bg: '/assets/collection/bst3-bg.jpg',
  bst3Street: '/assets/collection/bst3-street.jpg',

  // bst4 – Thêu hoa
  bst4Product: '/assets/collection/bst4-product.png',
  bst4Bg: '/assets/collection/bst4-bg.jpg',
  bst4CachTan: '/assets/collection/bst4-cach-tan.png',

  // bst5 – Cách tân
  bst5Reflection: '/assets/collection/bst5-reflection.png',
  bst5Product: '/assets/collection/bst5-product.png',
  bst5Bg: '/assets/collection/bst5-bg.jpg',
  bst5CachTan: '/assets/collection/bst5-cach-tan.png',

  // bst6 – Brand story
  bst6Texture: '/assets/collection/bst6-texture.jpg',

  // Gallery
  galleryPattern: '/assets/collection/gallery-pattern.png',
  galleryTexture: '/assets/collection/gallery-texture.jpg',
  galTT1: '/assets/collection/gal-truyen-thong-1.png',
  galTT2: '/assets/collection/gal-truyen-thong-2.png',
  galLT: '/assets/collection/gal-lua-tron.png',
  galTH1: '/assets/collection/gal-theu-hoa-1.png',
  galTH2: '/assets/collection/gal-theu-hoa-2.png',
  galCT: '/assets/collection/gal-cach-tan.png',
  galExtra: '/assets/collection/gal-extra.jpg',

  footerBg: '/assets/collection/footer-bg.jpg',
};

/* ── Gold gradient (matches Figma) ─────────────────── */
export const GOLD_GRADIENT =
  'linear-gradient(101.54deg, #ffd400 17.82%, #e2d48e 45.36%, #f6d948 56.66%, #e4c322 68.75%, #d7cc97 75.79%, #bea631 87.06%)';

/* ── Story intro (bst1) ────────────────────────────── */
export const STORY_INTRO = {
  title: 'Câu chuyện Áo dài',
  description:
    'Nhắc đến một trong những nét đẹp của truyền thống văn hóa Việt Nam thì chắc chắn không thể không nhắc đến Áo Dài. Trang phục này mang cội nguồn của dân tộc từ ngàn năm. Mỗi giai đoạn khác nhau, tà áo dài lại mang những dấu ấn riêng với điểm nhấn mới nhưng vẫn mang hồn cốt riêng của dân tộc Việt Nam. Dù vượt qua bao gian khó trong lịch sử đất nước, áo dài vẫn mang bản sắc, tinh thần dân tộc và chưa bao giờ mất đi sức sống độc đáo, kể từ khi ra đời.',
};

/* ── Era sections (bst2–bst5) ──────────────────────── */
export interface EraData {
  title: string;
  era: string;
  subtitle: string;
  description: string;
  layout: 'left' | 'right';
  images: { product: string; bg?: string; street?: string; cachTan?: string };
}

export const ERAS: EraData[] = [
  {
    title: 'Áo dài truyền thống cổ cao',
    era: 'Thập niên 1920 - 1940: Điểm tựa của Truyền thống',
    subtitle: 'Áo dài truyền thống – màu trầm, cổ cao kín đáo',
    description:
      'Đây là thời kỳ của những giá trị nền tảng. Trong bối cảnh xã hội xưa, chiếc áo dài màu nâu sồng, tím thẫm hay đen huyền với phần cổ cao nghiêm cẩn chính là biểu tượng của đức hạnh.',
    layout: 'right',
    images: { product: IMG.bst2Product, street: IMG.bst2Street },
  },
  {
    title: 'Áo dài lụa trơn',
    era: 'Thập niên 1950 - 1970: Bản tình ca của Lụa',
    subtitle: 'Áo dài lụa trơn – nhẹ nhàng, mềm mại, không cầu kỳ',
    description:
      'Khi nhịp sống bắt đầu len lỏi hơi thở của sự tự do, những tà áo lụa Hà Đông hay lụa tơ tằm trơn màu trắng, hồng phấn bắt đầu xuống phố. Không cần tầng tầng lớp lớp họa tiết, chính chất liệu lụa tự nhiên đã tôn vinh đường nét thanh xuân.',
    layout: 'left',
    images: { product: IMG.bst3Product, bg: IMG.bst3Bg, street: IMG.bst3Street },
  },
  {
    title: 'Áo dài thêu hoa',
    era: 'Thập niên 1980 - 2000: Đóa hoa của sự Kiên nhẫn',
    subtitle: 'Áo dài thêu hoa – từng cánh hoa được thêu tay tinh xảo',
    description:
      'Sau những năm tháng thăng trầm, đây là lúc nghệ thuật thủ công thăng hoa. Những chiếc áo dài được chăm chút tỉ mỉ với những mẫu thêu rực rỡ từ đôi bàn tay nghệ nhân trở thành chuẩn mực của sự sang trọng.',
    layout: 'right',
    images: { product: IMG.bst4Product, bg: IMG.bst4Bg, cachTan: IMG.bst4CachTan },
  },
  {
    title: 'Áo dài cách tân',
    era: 'Từ năm 2010 đến Tương lai: Tiếng nói của Sự tự do',
    subtitle: 'Áo dài cách tân – phóng khoáng, sáng tạo',
    description:
      'Bước vào kỷ nguyên số và hội nhập, chiếc áo dài không còn bị bó buộc trong những quy chuẩn cũ. Những đường cắt xẻ táo bạo, phối cùng váy xòe, quần lửng hay họa tiết đương đại đã ra đời.',
    layout: 'left',
    images: { product: IMG.bst5Product, bg: IMG.bst5Bg, cachTan: IMG.bst5CachTan },
  },
];

/* ── Brand story (bst6) ────────────────────────────── */
export const BRAND_STORY = {
  title: 'Câu chuyện Nhã Uyên',
  leftText:
    'Nhã Uyên là thương hiệu thời trang áo dài cao cấp, ra đời với sứ mệnh gìn giữ và tôn vinh những giá trị vĩnh cửu của trang phục dân tộc trong nhịp sống đương đại. Tên gọi "Nhã Uyên" đại diện cho sự kết hợp giữa nét thanh tao, nhã nhặn với chiều sâu trí tuệ và sự uyên bác trong nghệ thuật tạo hình. Với triết lý thẩm mỹ Cổ điển hiện đại, mỗi thiết kế của Nhã Uyên là một tác phẩm nghệ thuật tỉ mỉ, nơi những thước lụa truyền thống và kỹ thuật thêu tay tinh xảo gặp gỡ ngôn ngữ thiết kế mới mẻ.',
  rightText:
    'Không dừng lại ở một nhà may truyền thống, Nhã Uyên tiên phong định nghĩa lại trải nghiệm thời trang bằng cách tích hợp công nghệ trí tuệ nhân tạo AI Virtual Try-on, cho phép khách hàng khám phá và thử đồ ảo một cách chân thực ngay trên nền tảng số. Nhã Uyên không chỉ bán một chiếc áo dài, mà mang đến một hành trình kết nối: nơi quá khứ huy hoàng của di sản được tiếp nối bởi sự đột phá của tương lai, tạo nên một bản sắc riêng biệt, sang trọng và đầy tính bản sắc cho người phụ nữ Việt.',
};

/* ── Gallery collections (bst7–bst10) ──────────────── */
export interface CollectionItem {
  number: string;
  titleLines: string[];
  label: string;
  quote: string;
  images: string[];
}

export const COLLECTIONS: CollectionItem[] = [
  {
    number: 'No 1.',
    titleLines: ['ÁO DÀI', 'TRUYỀN   THỐNG'],
    label: 'NEW COLLECTION',
    quote: '"Cổ điển soi bóng, cốt cách trường tồn."',
    images: [IMG.galTT1, IMG.galTT2],
  },
  {
    number: 'No 2.',
    titleLines: ['ÁO DÀI', 'LỤA  TRƠN'],
    label: 'NEW COLLECTION',
    quote: '"Gói trọn thanh xuân, mượt mà nét Việt."',
    images: [IMG.galLT],
  },
  {
    number: 'No 3.',
    titleLines: ['ÁO DÀI', 'THÊU  HOA'],
    label: 'NEW COLLECTION',
    quote: '"Gấm hoa hội tụ, tuyệt tác thủ công."',
    images: [IMG.galTH1, IMG.galTH2],
  },
  {
    number: 'No 4.',
    titleLines: ['ÁO DÀI', 'CÁCH  TÂN'],
    label: 'NEW COLLECTION',
    quote: '"Phá vỡ giới hạn, định nghĩa thời đại."',
    images: [IMG.galCT, IMG.galExtra],
  },
];
