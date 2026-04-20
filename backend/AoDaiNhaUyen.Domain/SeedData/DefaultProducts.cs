namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultProducts
{
  private const string UploadPath = "/upload";

  public static readonly IReadOnlyList<SeedProduct> Items =
  [
    CreateAoDai("Áo dài cách tân 1", "ao-dai-cach-tan-1", "ao-dai-cach-tan", "gam-theu", 3250000m, true, "Áo dài cách tân màu đỏ đô với họa tiết thêu tinh tế", "Mẫu áo dài cách tân với thiết kế hiện đại, tôn dáng với đường cắt xẻ tà tinh tế. Vải gam thêu cao cấp tạo nên vẻ ngoài sang trọng, phù hợp cho các sự kiện trang trọng và tiệc cưới."),
    CreateAoDai("Áo dài cách tân 3", "ao-dai-cach-tan-3", "ao-dai-cach-tan", "gam-theu", 3350000m, true, "Áo dài cách tân màu xanh ngọc với họa tiết thêu hoa văn", "Mẫu áo dài với thiết kế hiện đại, đường cắt ôm body tôn dáng. Vải gam thêu cao cấp với họa tiết hoa văn tinh xảo, màu xanh ngọc tạo cảm giác thanh lịch và sang trọng, lý tưởng cho các dịp lễ hội."),
    CreateAoDai("Áo dài cách tân 4", "ao-dai-cach-tan-4", "ao-dai-cach-tan", "gam-theu", 3450000m, false, "Áo dài cách tân màu tím nhạt với thêu hoa cúc", "Mẫu áo dài với thiết kế tôn dáng, đường cắt xẻ tà tinh tế. Vải gam thêu cao cấp với thêu hoa cúc tinh xảo, màu tím nhạt tạo vẻ ngoài dịu dàng và sang trọng, phù hợp cho các buổi tiệc và chụp ảnh."),
    CreateAoDai("Áo dài cách tân 5", "ao-dai-cach-tan-5", "ao-dai-cach-tan", "gam-theu", 3550000m, false, "Áo dài cách tân màu đen với thêu kim tuyến lấp lánh", "Mẫu áo dài hiện đại với thiết kế tôn dáng, đường cắt xẻ tà ấn tượng. Vải gam thêu cao cấp với thêu kim tuyến lấp lánh tạo hiệu ứng sang trọng, màu đen tạo vẻ ngoài quyền lực và thanh lịch, lý tưởng cho các sự kiện tối cao."),
    CreateAoDai("Áo dài cách tân 6", "ao-dai-cach-tan-6", "ao-dai-cach-tan", "gam-theu", 3650000m, false, "Áo dài cách tân màu hồng pastel với thêu hoa hồng", "Mẫu áo dài với thiết kế tôn dáng, đường cắt xẻ tà thanh lịch. Vải gam thêu cao cấp với thêu hoa hồng tinh xảo, màu hồng pastel tạo vẻ ngoài ngọt ngào và sang trọng, phù hợp cho các buổi tiệc và sự kiện lãng mạn."),

    CreateAoDai("Áo dài lụa trơn 1", "ao-dai-lua-tron-1", "ao-dai-lua-tron", "lua-to-tam", 2850000m, true, "Áo dài lụa trơn màu hồng nhạt, thiết kế đơn giản thanh lịch", "Mẫu áo dài lụa trơn màu hồng nhạt với thiết kế đơn giản, đường cắt may tinh tế, chất liệu lụa tơ tằm mềm mại có độ bóng nhẹ tự nhiên, tạo nên vẻ đẹp thanh lịch và sang trọng cho người mặc."),
    CreateAoDai("Áo dài lụa trơn 2", "ao-dai-lua-tron-2", "ao-dai-lua-tron", "lua-to-tam", 2950000m, true, "Áo dài lụa trơn màu xanh dương đậm, thiết kế cổ tròn", "Mẫu áo dài lụa trơn màu xanh dương đậm với thiết kế cổ tròn cổ điển, đường may tinh xảo, chất liệu lụa tơ tằm có độ bóng mịn, tạo nên vẻ đẹp sang trọng và thanh lịch, phù hợp cho các dịp trang trọng."),
    CreateAoDai("Áo dài lụa trơn 3", "ao-dai-lua-tron-3", "ao-dai-lua-tron", "lua-to-tam", 3050000m, false, "Áo dài lụa trơn màu trắng ngà, thiết kế cổ V thanh lịch", "Mẫu áo dài lụa trơn màu trắng ngà với thiết kế cổ V thanh lịch, đường cắt may ôm body, chất liệu lụa tơ tằm có độ trong suốt nhẹ, tạo nên vẻ đẹp tinh khôi và sang trọng, phù hợp cho các dịp lễ tết."),
    CreateAoDai("Áo dài lụa trơn 4", "ao-dai-lua-tron-4", "ao-dai-lua-tron", "lua-to-tam", 3150000m, false, "Áo dài lụa trơn màu vàng kem, thiết kế cổ bẻ hiện đại", "Mẫu áo dài lụa trơn màu vàng kem với thiết kế cổ bẻ hiện đại, đường cắt may thon gọn, chất liệu lụa tơ tằm có độ bóng ấm, tạo nên vẻ đẹp quyến rũ và sang trọng, phù hợp cho các buổi tiệc cocktail."),
    CreateAoDai("Áo dài lụa trơn 5", "ao-dai-lua-tron-5", "ao-dai-lua-tron", "lua-to-tam", 3250000m, false, "Áo dài lụa trơn màu xanh ngọc, thiết kế cổ tròn cổ điển", "Mẫu áo dài lụa trơn màu xanh ngọc với thiết kế cổ tròn cổ điển, đường cắt may mềm mại, chất liệu lụa tơ tằm có độ bóng mượt, tạo nên vẻ đẹp thanh khiết và sang trọng, phù hợp cho các dịp lễ hội."),
    CreateAoDai("Áo dài lụa trơn 6", "ao-dai-lua-tron-6", "ao-dai-lua-tron", "lua-to-tam", 3350000m, false, "Áo dài lụa trơn màu tím nhạt, thiết kế cổ bẻ thanh lịch", "Mẫu áo dài lụa trơn màu tím nhạt với thiết kế cổ bẻ thanh lịch, đường cắt may ôm body, chất liệu lụa tơ tằm có độ trong suốt nhẹ, tạo nên vẻ đẹp lãng mạn và sang trọng, phù hợp cho các buổi tiệc tối."),
    CreateAoDai("Áo dài lụa trơn đặc biệt", "ao-dai-lua-tron-extra", "ao-dai-lua-tron", "lua-to-tam", 3450000m, false, "Áo dài lụa trơn màu đen tuyền, thiết kế cổ V cổ điển", "Mẫu áo dài lụa trơn màu đen tuyền với thiết kế cổ V cổ điển, đường cắt may tinh xảo, chất liệu lụa tơ tằm cao cấp có độ bóng mịn, tạo nên vẻ đẹp quyền lực và sang trọng, phù hợp cho các sự kiện quan trọng."),

    CreateAoDai("Áo dài thêu hoa 1", "ao-dai-theu-hoa-1", "ao-dai-theu-hoa", "gam-theu", 3850000m, true, "Áo dài thêu hoa tinh tế với hoa sen màu đỏ", "Áo dài được thêu thủ công với hoa sen màu đỏ tươi nổi bật trên nền lụa gam thêu mềm mại. Thiết kế cổ tròn truyền thống, tay áo dài, tà áo bay bổng với hoa sen thêu tỉ mỉ ở phần ngực và vai. Màu sắc tươi tắn, hoa văn tinh xảo, phù hợp cho các dịp lễ hội và tiệc tùng."),
    CreateAoDai("Áo dài thêu hoa 2", "ao-dai-theu-hoa-2", "ao-dai-theu-hoa", "gam-theu", 3950000m, true, "Áo dài thêu hoa sen màu hồng tinh khôi", "Áo dài lụa gam thêu cao cấp với hoa sen màu hồng nhạt thêu thủ công tinh xảo. Thiết kế cổ bẻ thanh lịch, tay áo ôm, tà áo dài với hoa sen thêu dọc theo đường may. Chất liệu lụa mềm mại, hoa văn trang nhã, phù hợp cho các dịp cưới hỏi và lễ tân."),
    CreateAoDai("Áo dài thêu hoa 3", "ao-dai-theu-hoa-3", "ao-dai-theu-hoa", "gam-theu", 4050000m, false, "Áo dài thêu hoa sen màu vàng rực rỡ", "Áo dài lụa gam thêu cao cấp với hoa sen vàng thêu thủ công tỉ mỉ. Thiết kế cổ tròn truyền thống, tay áo dài, tà áo bay bổng với hoa sen vàng thêu dọc theo đường may. Màu sắc rực rỡ, hoa văn phức tạp, chất liệu lụa đắt tiền, phù hợp cho các sự kiện trang trọng và tiệc tùng."),
    CreateAoDai("Áo dài thêu hoa 5", "ao-dai-theu-hoa-5", "ao-dai-theu-hoa", "gam-theu", 4150000m, false, "Áo dài thêu hoa sen màu xanh ngọc", "Áo dài lụa gam thêu cao cấp với hoa sen màu xanh ngọc thêu thủ công tinh xảo. Thiết kế cổ bẻ thanh lịch, tay áo ôm, tà áo dài với hoa sen thêu dọc theo đường may. Màu sắc tươi mát, hoa văn tinh tế, chất liệu lụa mềm mại, phù hợp cho các dịp đi chơi và gặp gỡ bạn bè."),
    CreateAoDai("Áo dài thêu hoa 6", "ao-dai-theu-hoa-6", "ao-dai-theu-hoa", "gam-theu", 4250000m, false, "Áo dài thêu hoa sen màu tím thẫm", "Áo dài lụa gam thêu cao cấp với hoa sen màu tím thẫm thêu thủ công phức tạp. Thiết kế cổ tròn truyền thống, tay áo dài, tà áo bay bổng với hoa sen tím thẫm thêu dọc theo đường may. Màu sắc sang trọng, hoa văn tinh xảo, chất liệu lụa đắt tiền, phù hợp cho các sự kiện trang trọng và tiệc tùng."),

    CreateAoDai("Áo dài truyền thống 1", "ao-dai-truyen-thong-1", "ao-dai-truyen-thong", "lua-to-tam", 3050000m, true, "Áo dài truyền thống màu đỏ thẫm với cổ tròn tinh tế", "Mẫu áo dài truyền thống với cổ tròn thanh lịch, đường cắt xẻ tà tinh tế. Vải lụa tơ tằm cao cấp với màu đỏ thẫm sang trọng, phù hợp cho các dịp lễ tết và sự kiện trang trọng, thể hiện vẻ đẹp truyền thống Việt Nam."),
    CreateAoDai("Áo dài truyền thống 2", "ao-dai-truyen-thong-2", "ao-dai-truyen-thong", "lua-to-tam", 3150000m, true, "Áo dài truyền thống màu xanh ngọc với họa tiết hoa văn", "Mẫu áo dài truyền thống với cổ tròn và đường cắt xẻ tà thanh lịch. Vải lụa tơ tằm màu xanh ngọc tinh tế, thêu hoa văn tinh xảo ở phần thân áo, tạo vẻ ngoài thanh thoát và sang trọng, lý tưởng cho các buổi tiệc và chụp ảnh."),
    CreateAoDai("Áo dài truyền thống 4", "ao-dai-truyen-thong-4", "ao-dai-truyen-thong", "lua-to-tam", 3250000m, false, "Áo dài truyền thống màu tím nhạt với thêu hoa cúc", "Mẫu áo dài truyền thống với cổ tròn và đường cắt xẻ tà tinh tế. Vải lụa tơ tằm màu tím nhạt dịu dàng, thêu hoa cúc tinh xảo ở phần thân áo, tạo vẻ ngoài thanh lịch và sang trọng, phù hợp cho các buổi tiệc và chụp ảnh."),
    CreateAoDai("Áo dài truyền thống 5", "ao-dai-truyen-thong-5", "ao-dai-truyen-thong", "lua-to-tam", 3350000m, false, "Áo dài truyền thống màu vàng óng với thêu kim tuyến", "Mẫu áo dài truyền thống với cổ tròn và đường cắt xẻ tà ấn tượng. Vải lụa tơ tằm màu vàng óng sang trọng, thêu kim tuyến lấp lánh tạo hiệu ứng thịnh soạn, phù hợp cho các sự kiện quan trọng và lễ hội."),
    CreateAoDai("Áo dài truyền thống 6", "ao-dai-truyen-thong-6", "ao-dai-truyen-thong", "lua-to-tam", 3450000m, false, "Áo dài truyền thống màu xanh lam với thêu hoa sen", "Mẫu áo dài truyền thống với cổ tròn và đường cắt xẻ tà thanh lịch. Vải lụa tơ tằm màu xanh lam sâu, thêu hoa sen tinh xảo ở phần thân áo, tạo vẻ ngoài thanh thoát và sang trọng, lý tưởng cho các buổi tiệc và sự kiện văn hóa."),
    CreateAoDai("Áo dài truyền thống nơ đỏ", "home-ao-dai-truyen-thong-node", "ao-dai-truyen-thong", "lua-to-tam", 3550000m, true, "Áo dài truyền thống màu đỏ với nơ ngực trang trí", "Mẫu áo dài truyền thống với cổ tròn và đường cắt xẻ tà tinh tế. Vải lụa tơ tằm màu đỏ rực rỡ, trang trí nơ ngực màu đỏ nổi bật, tạo vẻ ngoài quyền lực và sang trọng, phù hợp cho các sự kiện quan trọng và lễ hội."),
    CreateAoDai("Áo dài truyền thống", "home-ao-dai-truyen-thong", "ao-dai-truyen-thong", "lua-to-tam", 3650000m, true, "Áo dài truyền thống màu hồng pastel với thêu hoa hồng", "Mẫu áo dài truyền thống với cổ tròn và đường cắt xẻ tà thanh lịch. Vải lụa tơ tằm màu hồng pastel ngọt ngào, thêu hoa hồng tinh xảo ở phần thân áo, tạo vẻ ngoài dịu dàng và sang trọng, lý tưởng cho các buổi tiệc và sự kiện lãng mạn."),

    CreateAccessory("Quạt bầu", "phu-kien-quat-bau", "quat", 420000m, true, "Quạt bầu truyền thống với thiết kế cổ điển, phù hợp làm phụ kiện trang trọng", "Quạt bầu làm từ gỗ hoặc tre cao cấp, có hình dáng tròn đều với tay cầm dài. Thiết kế cổ điển với các nan quạt được sắp xếp đều đặn, tạo nên vẻ đẹp thanh lịch và trang trọng. Màu sắc tự nhiên của gỗ hoặc sơn mài tinh tế, phù hợp làm phụ kiện trang phục truyền thống và các dịp lễ tết quan trọng."),
    CreateAccessory("Quạt gấp tua rua", "phu-kien-quat-gap-tua-rua", "quat", 460000m, true, "Quạt gấp tua rua với thiết kế gấp gọn tiện dụng, hoa văn tinh xảo", "Quạt gấp tua rua có thiết kế gấp gọn thông minh với các nan quạt có thể mở rộng khi sử dụng. Vật liệu cao cấp từ tre hoặc gỗ, tay cầm được bọc lụa hoặc vải lụa sang trọng. Tua rua tinh xảo được thêu thủ công, tạo nên vẻ đẹp thanh lịch và tiện dụng cho các dịp đi chơi, du lịch hoặc các buổi tiệc ngoài trời."),
    CreateAccessory("Quạt hoa sen", "phu-kien-quat-hoa-sen", "quat", 440000m, false, "Quạt hoa sen với hoa văn thêu tinh xảo, thiết kế thanh lịch", "Quạt hoa sen được làm từ tre hoặc gỗ, với thiết kế nan quạt hình hoa sen tinh xảo. Tay cầm được chạm khắc hoa sen hoặc được bọc lụa màu đỏ, tượng trưng cho sự may mắn và phú quý. Màu sắc tự nhiên của tre hoặc sơn mài tinh tế, hoa văn thêu thủ công tinh xảo, tạo nên vẻ đẹp truyền thống và sang trọng, phù hợp cho các dịp lễ hội và sự kiện văn hóa."),
    CreateAccessory("Quạt tre gấp", "phu-kien-quat-tre-gap", "quat", 390000m, false, "Quạt tre gấp với thiết kế gấp gọn, làm từ tre tự nhiên", "Quạt tre gấp được làm hoàn toàn từ tre tự nhiên cao cấp, có thiết kế gấp gọn thông minh với các nan quạt có thể mở rộng khi sử dụng. Tay cầm được làm từ tre nguyên khối, tạo cảm giác chắc chắn và tự nhiên. Màu sắc tự nhiên của tre với vân gỗ rõ nét, thiết kế tối giản nhưng sang trọng, phù hợp cho các dịp đi chơi và du lịch."),
    CreateAccessory("Quạt tròn", "phu-kien-quat-tron", "quat", 410000m, false, "Quạt tròn truyền thống với thiết kế đơn giản, thanh lịch", "Quạt tròn có thiết kế đơn giản với nan quạt tròn đều đặn, làm từ tre hoặc gỗ cao cấp. Tay cầm dài tạo cảm giác cân bằng khi sử dụng. Màu sắc tự nhiên của tre hoặc sơn mài tinh tế, thiết kế cổ điển tạo nên vẻ đẹp thanh lịch và trang trọng. Quạt tròn là lựa chọn hoàn hảo cho các dịp lễ tết và sự kiện văn hóa."),
    CreateAccessory("Quạt vân mây", "phu-kien-quat-van-may", "quat", 450000m, false, "Quạt vân mây với thiết kế tinh xảo, họa tiết tự nhiên", "Quạt vân mây được làm từ tre hoặc gỗ với họa tiết vân mây tinh xảo, mô phỏng hình dáng của đám mây. Tay cầm được chạm khắc tinh tế, có thể được bọc lụa màu xanh nhạt tượng trưng cho bầu trời. Màu sắc tự nhiên của tre với vân gỗ đẹp mắt, thiết kế truyền thống nhưng độc đáo, phù hợp cho các dịp lễ hội và sự kiện văn hóa."),

    CreateAccessory("Guốc cao gót bướm trắng", "guoc-cao-got-buom-trang", "giay", 890000m, true, "Giày cao gót trắng với thiết kế bướm tinh tế", "Giày cao gót màu trắng tinh khôi với đế cao 8cm, đế bằng chắc chắn. Phần upper làm từ da bóng mịn, trang trí bằng nơ bướm ở mũi giày tạo điểm nhấn thanh lịch. Thiết kế cổ điển nhưng vẫn hiện đại, phù hợp với các dịp tiệc tùng và sự kiện trang trọng, kết hợp hoàn hảo với áo dài truyền thống và cách tân."),
    CreateAccessory("Guốc cao gót hoa cách điệu", "guoc-cao-got-hoa-cach-dieu", "giay", 920000m, true, "Giày cao gót hoa cách điệu với họa tiết tinh xảo", "Giày cao gót màu trắng với đế cao 10cm, thiết kế dáng thon gọn. Phần upper làm từ da bóng mịn, trang trí bằng hoa văn cách điệu tinh xảo ở mũi và thân giày. Màu sắc trắng tinh khôi kết hợp với hoa văn màu vàng nhạt, tạo nên vẻ đẹp thanh lịch và sang trọng, phù hợp cho các dịp lễ cưới và sự kiện quan trọng."),
    CreateAccessory("Guốc cao gót hoa", "guoc-cao-got-hoa", "giay", 860000m, false, "Giày cao gót hoa sen với thiết kế tinh xảo", "Giày cao gót màu trắng với đế cao 7cm, thiết kế dáng thon gọn. Phần upper làm từ da bóng mịn, trang trí bằng hoa sen thêu tay tinh xảo ở mũi và thân giày. Màu sắc trắng tinh khôi kết hợp với hoa sen màu hồng nhạt, tạo nên vẻ đẹp thanh lịch và sang trọng, phù hợp cho các dịp đi chơi và gặp gỡ bạn bè."),
    CreateAccessory("Guốc cao gót kiểu cao", "guoc-cao-got-kieu-cao", "giay", 960000m, false, "Giày cao gót kiểu cao với đế siêu cao", "Giày cao gót màu trắng với đế cao 12cm, thiết kế dáng thon gọn và đế mỏng. Phần upper làm từ da bóng mịn, có thiết kế đơn giản nhưng sang trọng. Màu sắc trắng tinh khôi, phù hợp cho các dịp tiệc tùng và sự kiện trang trọng, tạo dáng điệu thanh lịch và quyến rũ."),
    CreateAccessory("Guốc cao gót nơ đế bằng", "guoc-cao-got-no-de-bang", "giay", 880000m, false, "Giày cao gót nơ đế bằng với thiết kế tinh tế", "Giày cao gót màu trắng với đế cao 9cm, đế bằng chắc chắn. Phần upper làm từ da bóng mịn, trang trí bằng nơ lớn ở mũi giày tạo điểm nhấn thanh lịch. Thiết kế cổ điển nhưng vẫn hiện đại, phù hợp với các dịp tiệc tùng và sự kiện trang trọng, kết hợp hoàn hảo với áo dài truyền thống."),
    CreateAccessory("Guốc cao gót nơ", "guoc-cao-got-no", "giay", 900000m, false, "Giày cao gót nơ nhỏ với thiết kế thanh lịch", "Giày cao gót màu trắng với đế cao 8cm, đế bằng chắc chắn. Phần upper làm từ da bóng mịn, trang trí bằng nơ nhỏ ở mũi giày tạo điểm nhấn tinh tế. Thiết kế cổ điển nhưng vẫn hiện đại, phù hợp với các dịp tiệc tùng và sự kiện trang trọng, kết hợp hoàn hảo với áo dài truyền thống và cách tân."),

    CreateAccessory("Túi sách hoa văn", "phu-kien-tu-xach-hoa-van", "tui-sach", 680000m, true, "Túi sách hoa văn thiết kế tinh tế với họa tiết truyền thống", "Túi sách được làm từ vải lụa cao cấp với họa tiết hoa văn tinh xảo, thiết kế hình chữ nhật thanh lịch. Quai đeo được may tỉ mỉ, có nút cài trang trí hoa văn tương ứng. Kích thước vừa vặn, phù hợp đựng sách và phụ kiện cá nhân, thể hiện vẻ đẹp truyền thống và sang trọng."),
    CreateAccessory("Túi sách kiểu", "phu-kien-tu-xach-kieu", "tui-sach", 720000m, true, "Túi sách kiểu hiện đại với thiết kế độc đáo, phù hợp đi chơi", "Túi sách kiểu được làm từ vải lụa cao cấp với thiết kế hiện đại, có hình dáng độc đáo và thời trang. Quai đeo được thiết kế ergonomic, dễ dàng sử dụng. Kích thước vừa vặn, có ngăn đựng sách và phụ kiện, phù hợp cho các dịp đi chơi, du lịch và các sự kiện ngoài trời."),
    CreateAccessory("Túi sách thanh", "phu-kien-tu-xach-thanh", "tui-sach", 650000m, false, "Túi sách thanh mảnh thiết kế tối giản, sang trọng", "Túi sách thanh mảnh được làm từ vải lụa mỏng nhẹ, có thiết kế tối giản và thanh lịch. Hình dáng mảnh mai, dễ dàng đựng trong túi xách hoặc cầm tay. Quai đeo được may tỉ mỉ, có nút cài tinh tế. Kích thước nhỏ gọn, phù hợp đựng sách nhỏ, điện thoại và các vật dụng cá nhân hàng ngày."),
    CreateAccessory("Túi sách thêu hoa", "phu-kien-tu-xach-theu-hoa", "tui-sach", 760000m, false, "Túi sách thêu hoa tinh xảo, họa tiết hoa sen", "Túi sách được làm từ vải lụa cao cấp với thêu hoa sen tinh xảo bằng tay. Thiết kế hình chữ nhật thanh lịch, có hoa sen thêu nổi bật ở phần trước túi. Quai đeo được may tỉ mỉ, có nút cài trang trí hoa sen tương ứng. Kích thước vừa vặn, phù hợp đựng sách và phụ kiện, thể hiện vẻ đẹp truyền thống và tinh tế."),
    CreateAccessory("Túi sách thức", "phu-kien-tu-xach-thuc", "tui-sach", 690000m, false, "Túi sách thức lớn thiết kế rộng rãi, tiện dụng", "Túi sách thức lớn được làm từ vải lụa dày dặn, có thiết kế rộng rãi và tiện dụng. Hình dáng vuông vức, có quai đeo chắc chắn và nút cài an toàn. Kích thước lớn, phù hợp đựng sách dày, tài liệu quan trọng và các vật dụng cá nhân cần thiết. Thiết kế truyền thống nhưng rất thực tế."),
    CreateAccessory("Túi sách xà cừ", "phu-kien-tu-xach-xan-oc", "tui-sach", 790000m, false, "Túi sách xà cừ cao cấp với họa tiết xà cừ tinh xảo", "Túi sách xà cừ được làm từ vải lụa cao cấp với họa tiết xà cừ tinh xảo, mô phỏng vân vảy của con rồng. Thiết kế hình chữ nhật thanh lịch, có quai đeo được may tỉ mỉ với nút cài trang trí xà cừ. Kích thước vừa vặn, phù hợp đựng sách và phụ kiện, thể hiện vẻ đẹp sang trọng và quyền lực, phù hợp cho các sự kiện quan trọng."),

    CreateAccessory("Trâm cài hoa đơn 2", "tram-cai-hoa-don-2", "tram-cai", 360000m, true, "Trâm cài hoa đơn 2 với thiết kế tinh xảo, phù hợp cho các dịp trang trọng", "Trâm cài hoa đơn 2 được chế tác từ bạc tinh khiết với hoa đơn thêu tay tinh xảo. Thiết kế cổ điển với hoa văn hoa đơn nổi bật, màu sắc trang nhã, tạo nên vẻ đẹp thanh lịch và sang trọng, lý tưởng để trang trí cho tóc trong các buổi lễ cưới và tiệc tùng quan trọng."),
    CreateAccessory("Trâm cài hoa đơn sắc", "tram-cai-hoa-don-sac", "tram-cai", 340000m, true, "Trâm cài hoa đơn sắc với thiết kế đơn giản thanh lịch, phù hợp cho các dịp trang trọng", "Trâm cài hoa đơn sắc được chế tác từ bạc tinh khiết với hoa đơn thêu tay tinh xảo trên nền màu đồng nhất. Thiết kế cổ điển với hoa văn hoa đơn đơn sắc, tạo nên vẻ đẹp thanh lịch và sang trọng, lý tưởng để trang trí cho tóc trong các buổi lễ cưới và tiệc tùng quan trọng."),
    CreateAccessory("Trâm cài hoa đơn", "tram-cai-hoa-don", "tram-cai", 320000m, false, "Trâm cài hoa đơn với thiết kế cổ điển, phù hợp cho các dịp trang trọng", "Trâm cài hoa đơn được chế tác từ bạc tinh khiết với hoa đơn thêu tay tinh xảo. Thiết kế cổ điển với hoa văn hoa đơn nổi bật, màu sắc trang nhã, tạo nên vẻ đẹp thanh lịch và sang trọng, lý tưởng để trang trí cho tóc trong các buổi lễ cưới và tiệc tùng quan trọng."),
    CreateAccessory("Trâm cài tóc hồng ngọc bích", "tram-cai-toc-hong-ngoc-bich", "tram-cai", 390000m, false, "Trâm cài tóc hồng ngọc bích với thiết kế tinh xảo, màu sắc nổi bật", "Trâm cài tóc hồng ngọc bích được chế tác từ bạc tinh khiết với hoa văn thêu tay tinh xảo và ngọc bích hồng tự nhiên. Thiết kế hiện đại với hoa văn hoa văn phức tạp, màu sắc ngọc bích hồng rực rỡ, tạo nên vẻ đẹp quyến rũ và sang trọng, lý tưởng để trang trí cho tóc trong các buổi tiệc và sự kiện quan trọng."),
    CreateAccessory("Trâm cài tóc vàng sắc điệu", "tram-cai-toc-vang-sac-dieu", "tram-cai", 380000m, false, "Trâm cài tóc vàng sắc điệu với thiết kế tinh xảo, màu sắc vàng óng", "Trâm cài tóc vàng sắc điệu được chế tác từ bạc tinh khiết với hoa văn thêu tay tinh xảo và sắc điệu vàng óng. Thiết kế hiện đại với hoa văn hoa văn phức tạp, màu sắc vàng óng sang trọng, tạo nên vẻ đẹp quý phái và thanh lịch, lý tưởng để trang trí cho tóc trong các buổi tiệc và sự kiện quan trọng."),
    CreateAccessory("Trâm cài tóc xanh biếc", "tram-cai-toc-xanh-biec", "tram-cai", 370000m, false, "Trâm cài tóc xanh biếc với thiết kế tinh xảo, màu sắc xanh biếc thanh lịch", "Trâm cài tóc xanh biếc được chế tác từ bạc tinh khiết với hoa văn thêu tay tinh xảo và sắc xanh biếc tự nhiên. Thiết kế hiện đại với hoa văn hoa văn phức tạp, màu sắc xanh biếc thanh lịch, tạo nên vẻ đẹp trong suốt và sang trọng, lý tưởng để trang trí cho tóc trong các buổi tiệc và sự kiện quan trọng.")
  ];

  private static SeedProduct CreateAoDai(
    string name,
    string imageSlug,
    string categorySlug,
    string materialSlug,
    decimal price,
    bool isFeatured,
    string? shortDescription = null,
    string? longDescription = null)
  {
    return new SeedProduct(
      name,
      imageSlug,
      shortDescription ?? "Thiết kế áo dài Nha Uyên may sẵn.",
      longDescription ?? "Mẫu áo dài được chọn lọc theo tinh thần thanh lịch, phù hợp đi tiệc, chụp ảnh và các dịp trang trọng.",
      price,
      "VND",
      categorySlug,
      materialSlug,
      isFeatured,
      [CreateImage(imageSlug, name)],
      [
        CreateVariant(imageSlug, "S", "S", price, 8, true),
        CreateVariant(imageSlug, "M", "M", price + 150000m, 10, false),
        CreateVariant(imageSlug, "L", "L", price + 300000m, 6, false)
      ]);
  }

  private static SeedProduct CreateAccessory(
    string name,
    string imageSlug,
    string categorySlug,
    decimal price,
    bool isFeatured,
    string? shortDescription = null,
    string? longDescription = null)
  {
    return new SeedProduct(
      name,
      imageSlug,
      shortDescription ?? "Phụ kiện phối cùng áo dài.",
      longDescription ?? "Phụ kiện hoàn thiện tổng thể trang phục, phù hợp dùng cùng các bộ áo dài truyền thống và cách tân.",
      price,
      "VND",
      categorySlug,
      null,
      isFeatured,
      [CreateImage(imageSlug, name)],
      [CreateVariant(imageSlug, "STD", null, price, 15, true)]);
  }

  private static SeedProductImage CreateImage(string slug, string altText)
  {
    return new SeedProductImage($"{UploadPath}/{slug}.webp", altText, 1, true);
  }

  private static SeedProductVariant CreateVariant(
    string productSlug,
    string skuSuffix,
    string? size,
    decimal price,
    int stockQty,
    bool isDefault)
  {
    var sku = $"{productSlug}-{skuSuffix}".ToUpperInvariant().Replace('-', '_');
    var variantName = size is null ? "Mặc định" : $"Size {size}";
    return new SeedProductVariant(sku, variantName, size, null, price, null, stockQty, isDefault);
  }
}

public sealed record SeedProduct(
  string Name,
  string Slug,
  string ShortDescription,
  string LongDescription,
  decimal Price,
  string Currency,
  string CategorySlug,
  string? MaterialSlug,
  bool IsFeatured,
  IReadOnlyList<SeedProductImage> Images,
  IReadOnlyList<SeedProductVariant> Variants);

public sealed record SeedProductImage(string ImageUrl, string AltText, int SortOrder, bool IsPrimary);

public sealed record SeedProductVariant(
  string Sku,
  string VariantName,
  string? Size,
  string? Color,
  decimal Price,
  decimal? SalePrice,
  int StockQty,
  bool IsDefault);
