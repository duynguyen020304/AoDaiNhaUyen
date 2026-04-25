export type ProductSectionNoteId = 'embroidered-detail' | 'bodice-form' | 'handbag-detail';

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
  spotImages: Partial<Record<ProductSectionNoteId, string>>;
  notePlacements: Partial<Record<ProductSectionNoteId, ProductSectionNotePlacement>>;
};

export type ProductSectionNotePlacement = {
  hotspotX: string;
  hotspotY: string;
  calloutX: string;
  calloutY: string;
  calloutObjectPosition?: string;
  connectorLength: string;
  connectorAngle: string;
};

export type ProductSectionNote = {
  id: ProductSectionNoteId;
  label: string;
  title: string;
  lines: string[];
  hotspotX: string;
  hotspotY: string;
  calloutX: string;
  calloutY: string;
  calloutObjectPosition: string;
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
    connectorLength: '154px',
    connectorAngle: '-4deg',
  },
];

const productSectionAssetBase = '/assets/product-section';

export const productSectionSlides: ProductSectionSlide[] = [
  {
    id: 'white-floral',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI TRẮNG',
    headlineLines: ['ÁO DÀI', 'TRẮNG'],
    previewLabel: 'Áo dài trắng phối hoa ly',
    heroImage: `${productSectionAssetBase}/trắng.png`,
    heroAlt: 'Mẫu áo dài trắng cầm hoa ly',
    previewImage: `${productSectionAssetBase}/trắng full.png`,
    previewShadowImage: `${productSectionAssetBase}/trắng full.png`,
    previewAlt: 'Áo dài trắng cầm hoa ly trên lối đi',
    thumbnailImage: `${productSectionAssetBase}/trắng full.png`,
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Sắc trắng tinh giản, cổ cao truyền thống và tay dài ôm nhẹ giúp tổng thể thanh thoát, kín đáo và phù hợp nhiều bối cảnh lễ nghi.',
      'Bó hoa ly được dùng như điểm nhấn mềm để giữ hình ảnh nhẹ nhàng nhưng vẫn đủ trang trọng trong khung carousel.',
      '2. Thành phần phối bộ',
      'Áo dài trắng phối quần lụa cùng tông, nón lá và hoa cầm tay để tạo cảm giác cổ điển, sạch màu, không lấn át form áo.',
      '3. Phong cách chủ đạo',
      'Phù hợp ảnh lookbook ngoài trời, lễ kỷ niệm và những concept cần nét trong trẻo, chuẩn mực.',
    ],
    spotImages: {
      'embroidered-detail': `${productSectionAssetBase}/trắng .1.png`,
      'bodice-form': `${productSectionAssetBase}/trắng 2.jpg`,
      'handbag-detail': `${productSectionAssetBase}/trắng 3.jpg`,
    },
    notePlacements: {
      'embroidered-detail': {
        hotspotX: '48%',
        hotspotY: '28%',
        calloutX: '66%',
        calloutY: '20%',
        connectorLength: '120px',
        connectorAngle: '190deg',
      },
      'bodice-form': {
        hotspotX: '42%',
        hotspotY: '40%',
        calloutX: '-36%',
        calloutY: '32%',
        connectorLength: '126px',
        connectorAngle: '0deg',
      },
      'handbag-detail': {
        hotspotX: '48%',
        hotspotY: '86%',
        calloutX: '62%',
        calloutY: '70%',
        connectorLength: '132px',
        connectorAngle: '210deg',
      },
    },
  },
  {
    id: 'ruby-wood',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI ĐỎ',
    headlineLines: ['ÁO DÀI', 'ĐỎ'],
    previewLabel: 'Áo dài đỏ trong không gian gỗ',
    heroImage: `${productSectionAssetBase}/đỏ.png`,
    heroAlt: 'Mẫu áo dài đỏ trong không gian nội thất gỗ',
    previewImage: `${productSectionAssetBase}/đỏ full.png`,
    previewShadowImage: `${productSectionAssetBase}/đỏ full.png`,
    previewAlt: 'Áo dài đỏ thêu hạc trong không gian gỗ',
    thumbnailImage: `${productSectionAssetBase}/đỏ full.png`,
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Tông đỏ trầm nổi bật trên nền gỗ ấm, giữ đúng tinh thần áo dài truyền thống với cổ cao, tay dài và thân áo suông thanh lịch.',
      'Chi tiết thêu hạc trắng tạo nhịp sáng vừa đủ, giúp thẻ thứ hai vẫn rõ màu đỏ như mẫu mà không bị nhuộm đỏ toàn khung.',
      '2. Thành phần phối bộ',
      'Phối cùng quần lụa đỏ, hoa cài tóc và bố cục nội thất mộc để nhấn vào chất liệu, màu sắc và chiều sâu bối cảnh.',
      '3. Phong cách chủ đạo',
      'Dành cho dịp lễ, Tết và các bộ ảnh cần sắc đỏ sang nhưng không quá chói.',
    ],
    spotImages: {
      'embroidered-detail': `${productSectionAssetBase}/đỏ 1.png`,
      'bodice-form': `${productSectionAssetBase}/đỏ 2.png`,
      'handbag-detail': `${productSectionAssetBase}/đỏ 3.jpg`,
    },
    notePlacements: {
      'embroidered-detail': {
        hotspotX: '60%',
        hotspotY: '10%',
        calloutX: '70%',
        calloutY: '10%',
        connectorLength: '120px',
        connectorAngle: '188deg',
      },
      'bodice-form': {
        hotspotX: '42%',
        hotspotY: '26%',
        calloutX: '-36%',
        calloutY: '20%',
        connectorLength: '130px',
        connectorAngle: '4deg',
      },
      'handbag-detail': {
        hotspotX: '62%',
        hotspotY: '62%',
        calloutX: '70%',
        calloutY: '54%',
        connectorLength: '110px',
        connectorAngle: '192deg',
      },
    },
  },
  {
    id: 'pink-embroidered',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI THÊU HOA',
    headlineLines: ['ÁO DÀI', 'THÊU HOA'],
    previewLabel: 'Áo dài hồng thêu hoa nội thất',
    heroImage: `${productSectionAssetBase}/hồng.png`,
    heroAlt: 'Mẫu áo dài hồng thêu hoa trong không gian nội thất',
    previewImage: `${productSectionAssetBase}/hồng full.png`,
    previewShadowImage: `${productSectionAssetBase}/hồng full.png`,
    previewAlt: 'Áo dài hồng thêu hoa trong không gian nội thất sang trọng',
    thumbnailImage: `${productSectionAssetBase}/hồng full.png`,
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Nền hồng phấn thêu hoa trải dọc thân áo, tay loe mềm và phom dài tạo cảm giác sang trọng, đúng trọng tâm của thẻ trung tâm.',
      'Không gian nội thất sáng màu làm nổi phần thêu, tránh cảm giác accessory hay chi tiết phụ lấn át sản phẩm chính.',
      '2. Thành phần phối bộ',
      'Áo dài phối quần trắng kem, tóc thả nhẹ và phụ kiện tối giản để người xem tập trung vào đường thêu và chất vải.',
      '3. Phong cách chủ đạo',
      'Đây là hình ảnh chủ lực của carousel, phù hợp cho lookbook áo dài thêu hoa cao cấp.',
    ],
    spotImages: {
      'embroidered-detail': `${productSectionAssetBase}/hồng 1.png`,
      'bodice-form': `${productSectionAssetBase}/Hồng 2.jpg`,
      'handbag-detail': `${productSectionAssetBase}/Hồng 3.jpg`,
    },
    notePlacements: {
      'embroidered-detail': {
        hotspotX: '50%',
        hotspotY: '28%',
        calloutX: '-36%',
        calloutY: '22%',
        connectorLength: '132px',
        connectorAngle: '2deg',
      },
      'bodice-form': {
        hotspotX: '52%',
        hotspotY: '38%',
        calloutX: '70%',
        calloutY: '34%',
        connectorLength: '110px',
        connectorAngle: '194deg',
      },
      'handbag-detail': {
        hotspotX: '56%',
        hotspotY: '86%',
        calloutX: '-38%',
        calloutY: '70%',
        connectorLength: '150px',
        connectorAngle: '-6deg',
      },
    },
  },
  {
    id: 'powder-blue',
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'ÁO DÀI XANH',
    headlineLines: ['ÁO DÀI', 'XANH'],
    previewLabel: 'Áo dài xanh da trời trên bậc thang',
    heroImage: `${productSectionAssetBase}/xanh.png`,
    heroAlt: 'Mẫu áo dài xanh da trời trên bậc thang',
    previewImage: `${productSectionAssetBase}/xanh full.png`,
    previewShadowImage: `${productSectionAssetBase}/xanh full.png`,
    previewAlt: 'Áo dài xanh da trời chụp trên bậc thang',
    thumbnailImage: `${productSectionAssetBase}/xanh full.png`,
    detailLines: [
      '1. Đặc điểm thiết kế',
      'Màu xanh da trời dịu, chất vải trơn và phom cổ cao tạo một thẻ phụ sáng màu, cân bằng với đỏ và hồng trong toàn bộ rail.',
      'Bối cảnh bậc thang giữ chiều sâu ảnh nhưng vẫn đủ sạch để nhận diện sản phẩm ở kích thước thumbnail.',
      '2. Thành phần phối bộ',
      'Áo dài xanh phối quần trắng, tóc buông tự nhiên và phụ kiện tiết chế để giữ tổng thể nhẹ, thanh.',
      '3. Phong cách chủ đạo',
      'Phù hợp các concept thanh lịch, trẻ trung và tạo điểm nghỉ thị giác sau thẻ trung tâm.',
    ],
    spotImages: {
      'embroidered-detail': `${productSectionAssetBase}/xanh 2.webp`,
      'bodice-form': `${productSectionAssetBase}/xanh 1.png`,
      'handbag-detail': `${productSectionAssetBase}/xanh 3.jpg`,
    },
    notePlacements: {
      'embroidered-detail': {
        hotspotX: '36%',
        hotspotY: '12%',
        calloutX: '-36%',
        calloutY: '10%',
        connectorLength: '136px',
        connectorAngle: '2deg',
      },
      'bodice-form': {
        hotspotX: '52%',
        hotspotY: '30%',
        calloutX: '72%',
        calloutY: '28%',
        connectorLength: '112px',
        connectorAngle: '190deg',
      },
      'handbag-detail': {
        hotspotX: '47%',
        hotspotY: '55%',
        calloutX: '-38%',
        calloutY: '48%',
        connectorLength: '142px',
        connectorAngle: '0deg',
      },
    },
  },
];
