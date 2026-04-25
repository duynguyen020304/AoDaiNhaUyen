export type ProductSectionSlide = {
  id: string;
  eyebrow: string;
  title: string;
  headlineLines: string[];
  previewLabel: string;
  heroImage: string;
  heroAlt: string;
  previewImage: string;
  previewShadowImage: string;
  previewAlt: string;
  thumbnailImage: string;
  detailLines: string[];
};

export type ProductSectionNote = {
  id: string;
  label: string;
  title: string;
  lines: string[];
  hotspotX: string;
  hotspotY: string;
  calloutX: string;
  calloutY: string;
  calloutObjectPosition: string;
  calloutScale: number;
  connectorLength: string;
  connectorAngle: string;
};

export const productSectionNotes: ProductSectionNote[] = [
  {
    id: 'embroidered-detail',
    label: 'Xem chi tiết họa tiết thêu',
    title: 'Họa tiết thêu',
    lines: [
      'Các mảng thêu được đặt theo chiều dọc thân áo để kéo dài dáng người và giữ nhịp nhìn mềm mại trên nền vải.',
      'Độ tương phản vừa phải giúp chi tiết nổi lên khi nhìn gần nhưng vẫn giữ tổng thể thanh lịch trong khung carousel.',
    ],
    hotspotX: '14%',
    hotspotY: '18%',
    calloutX: '-34%',
    calloutY: '20%',
    calloutObjectPosition: '48% 12%',
    calloutScale: 2.5,
    connectorLength: '132px',
    connectorAngle: '6deg',
  },
  {
    id: 'bodice-form',
    label: 'Xem chi tiết phom áo',
    title: 'Phom áo',
    lines: [
      'Phần thân áo tập trung vào đường cắt gọn và tỷ lệ ôm vừa phải để tôn dáng nhưng vẫn giữ được sự kín đáo.',
      'Chất liệu có độ rủ mềm giúp bề mặt áo ít gãy nếp, tạo cảm giác thanh thoát khi người mẫu di chuyển.',
    ],
    hotspotX: '50%',
    hotspotY: '31%',
    calloutX: '72%',
    calloutY: '38%',
    calloutObjectPosition: '50% 35%',
    calloutScale: 2.7,
    connectorLength: '112px',
    connectorAngle: '198deg',
  },
  {
    id: 'handbag-detail',
    label: 'Xem chi tiết phụ kiện cầm tay',
    title: 'Phụ kiện cầm tay',
    lines: [
      'Túi hoặc hoa cầm tay được dùng như điểm nhấn ở nửa dưới khung hình, giúp cân bằng dáng đứng và màu sắc trang phục.',
      'Các chi tiết xếp nếp, quai vòng hoặc chất liệu sáng màu tạo thêm nhịp hiện đại cho tổng thể áo dài.',
    ],
    hotspotX: '18%',
    hotspotY: '64%',
    calloutX: '-38%',
    calloutY: '63%',
    calloutObjectPosition: '45% 64%',
    calloutScale: 2.8,
    connectorLength: '154px',
    connectorAngle: '-4deg',
  },
];

export const productSectionSlides: ProductSectionSlide[] = [
  {
    id: 'white-floral',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI TRẮNG',
    headlineLines: ['ÁO DÀI', 'TRẮNG'],
    previewLabel: 'Áo dài trắng phối hoa ly',
    heroImage: '/assets/products/product-truyen-thong-5.png',
    heroAlt: 'Mẫu áo dài trắng cầm hoa ly',
    previewImage: '/assets/products/product-truyen-thong-5.png',
    previewShadowImage: '/assets/dress-panel.png',
    previewAlt: 'Áo dài trắng cầm hoa ly trên lối đi',
    thumbnailImage: '/assets/products/product-truyen-thong-5.png',
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Sắc trắng tinh giản, cổ cao truyền thống và tay dài ôm nhẹ giúp tổng thể thanh thoát, kín đáo và phù hợp nhiều bối cảnh lễ nghi.',
      'Bó hoa ly được dùng như điểm nhấn mềm để giữ hình ảnh nhẹ nhàng nhưng vẫn đủ trang trọng trong khung carousel.',
      '2. Thành phần phối bộ',
      'Áo dài trắng phối quần lụa cùng tông, nón lá và hoa cầm tay để tạo cảm giác cổ điển, sạch màu, không lấn át form áo.',
      '3. Phong cách chủ đạo',
      'Phù hợp ảnh lookbook ngoài trời, lễ kỷ niệm và những concept cần nét trong trẻo, chuẩn mực.',
    ],
  },
  {
    id: 'ruby-wood',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI ĐỎ',
    headlineLines: ['ÁO DÀI', 'ĐỎ'],
    previewLabel: 'Áo dài đỏ trong không gian gỗ',
    heroImage: '/assets/products/product-truyen-thong-1.png',
    heroAlt: 'Mẫu áo dài đỏ trong không gian nội thất gỗ',
    previewImage: '/assets/products/product-truyen-thong-1.png',
    previewShadowImage: '/assets/dress-panel.png',
    previewAlt: 'Áo dài đỏ thêu hạc trong không gian gỗ',
    thumbnailImage: '/assets/products/product-truyen-thong-1.png',
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Tông đỏ trầm nổi bật trên nền gỗ ấm, giữ đúng tinh thần áo dài truyền thống với cổ cao, tay dài và thân áo suông thanh lịch.',
      'Chi tiết thêu hạc trắng tạo nhịp sáng vừa đủ, giúp thẻ thứ hai vẫn rõ màu đỏ như mẫu mà không bị nhuộm đỏ toàn khung.',
      '2. Thành phần phối bộ',
      'Phối cùng quần lụa đỏ, hoa cài tóc và bố cục nội thất mộc để nhấn vào chất liệu, màu sắc và chiều sâu bối cảnh.',
      '3. Phong cách chủ đạo',
      'Dành cho dịp lễ, Tết và các bộ ảnh cần sắc đỏ sang nhưng không quá chói.',
    ],
  },
  {
    id: 'pink-embroidered',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI THÊU HOA',
    headlineLines: ['ÁO DÀI', 'THÊU HOA'],
    previewLabel: 'Áo dài hồng thêu hoa nội thất',
    heroImage: '/assets/products/product-theu-hoa-3.png',
    heroAlt: 'Mẫu áo dài hồng thêu hoa trong không gian nội thất',
    previewImage: '/assets/products/product-theu-hoa-3.png',
    previewShadowImage: '/assets/dress-panel.png',
    previewAlt: 'Áo dài hồng thêu hoa trong không gian nội thất sang trọng',
    thumbnailImage: '/assets/products/product-theu-hoa-3.png',
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Nền hồng phấn thêu hoa trải dọc thân áo, tay loe mềm và phom dài tạo cảm giác sang trọng, đúng trọng tâm của thẻ trung tâm.',
      'Không gian nội thất sáng màu làm nổi phần thêu, tránh cảm giác accessory hay chi tiết phụ lấn át sản phẩm chính.',
      '2. Thành phần phối bộ',
      'Áo dài phối quần trắng kem, tóc thả nhẹ và phụ kiện tối giản để người xem tập trung vào đường thêu và chất vải.',
      '3. Phong cách chủ đạo',
      'Đây là hình ảnh chủ lực của carousel, phù hợp cho lookbook áo dài thêu hoa cao cấp.',
    ],
  },
  {
    id: 'powder-blue',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI XANH',
    headlineLines: ['ÁO DÀI', 'XANH'],
    previewLabel: 'Áo dài xanh da trời trên bậc thang',
    heroImage: '/assets/products/product-lua-tron-4.png',
    heroAlt: 'Mẫu áo dài xanh da trời trên bậc thang',
    previewImage: '/assets/products/product-lua-tron-4.png',
    previewShadowImage: '/assets/dress-panel.png',
    previewAlt: 'Áo dài xanh da trời chụp trên bậc thang',
    thumbnailImage: '/assets/products/product-lua-tron-4.png',
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Màu xanh da trời dịu, chất vải trơn và phom cổ cao tạo một thẻ phụ sáng màu, cân bằng với đỏ và hồng trong toàn bộ rail.',
      'Bối cảnh bậc thang giữ chiều sâu ảnh nhưng vẫn đủ sạch để nhận diện sản phẩm ở kích thước thumbnail.',
      '2. Thành phần phối bộ',
      'Áo dài xanh phối quần trắng, tóc buông tự nhiên và phụ kiện tiết chế để giữ tổng thể nhẹ, thanh.',
      '3. Phong cách chủ đạo',
      'Phù hợp các concept thanh lịch, trẻ trung và tạo điểm nghỉ thị giác sau thẻ trung tâm.',
    ],
  },
  {
    id: 'ivory-modern',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI TRẮNG',
    headlineLines: ['ÁO DÀI', 'TRẮNG'],
    previewLabel: 'Áo dài trắng dáng hiện đại',
    heroImage: '/assets/products/product-lua-tron-5.png',
    heroAlt: 'Mẫu áo dài trắng dáng hiện đại',
    previewImage: '/assets/products/product-lua-tron-5.png',
    previewShadowImage: '/assets/dress-panel.png',
    previewAlt: 'Áo dài trắng dáng hiện đại trên nền tối giản',
    thumbnailImage: '/assets/products/product-lua-tron-5.png',
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Một biến thể trắng khác với phom gọn, nền tối giản và biểu cảm hiện đại để thẻ ngoài cùng không bị trùng cảm giác với ảnh đầu.',
      'Tỷ lệ người mẫu đứng thẳng giúp thumbnail vẫn đọc rõ dáng áo dù bị thu nhỏ và làm tối nhẹ.',
      '2. Thành phần phối bộ',
      'Áo dài trắng phối quần đồng màu, giữ phụ kiện gần như tối thiểu để nhấn vào đường cắt và chất vải.',
      '3. Phong cách chủ đạo',
      'Dành cho khách thích vẻ tối giản, trang nhã và dễ ứng dụng trong sự kiện ban ngày.',
    ],
  },
];
