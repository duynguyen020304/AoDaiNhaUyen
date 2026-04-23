using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class StylistFallbackTextService : IStylistFallbackTextService
{
  private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Messages = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
  {
    ["thread_welcome"] =
    [
      "Mình là stylist AI của Nhã Uyên. Bạn đang tìm áo dài cho dịp nào để mình gợi ý đúng hơn?",
      "Mình có thể giúp bạn chọn áo dài, phối phụ kiện hoặc chuẩn bị thử đồ AI. Bạn đang cần cho dịp nào?",
      "Bạn cứ nói dịp mặc, màu bạn thích hoặc ngân sách dự kiến, mình sẽ lọc mẫu phù hợp cho bạn.",
      "Nếu muốn bắt đầu nhanh, bạn chỉ cần cho mình biết dịp mặc và tone màu bạn đang nghiêng về.",
      "Mình sẵn sàng gợi ý mẫu áo dài theo dịp, màu và chất liệu bạn thích. Bạn muốn bắt đầu từ đâu?",
      "Bạn đang cần một mẫu để đi dạy, đi tiệc, chụp ảnh hay dịp lễ Tết? Mình sẽ gợi ý theo đúng bối cảnh.",
      "Bạn mô tả sơ dịp mặc, ngân sách hoặc gu màu sắc, mình sẽ chọn giúp vài hướng hợp trước.",
      "Mình có thể tư vấn theo catalog hiện có hoặc hỗ trợ thử đồ bằng ảnh. Bạn muốn xem mẫu hay thử đồ trước?",
      "Mình sẽ giúp bạn thu hẹp lựa chọn nhanh hơn nếu biết dịp mặc, màu yêu thích hoặc chất liệu bạn muốn.",
      "Cứ chia sẻ nhu cầu của bạn như dịp mặc, màu sắc hoặc ảnh người mặc, mình sẽ hỗ trợ từng bước."
    ],
    ["clarification"] =
    [
      "Để mình tư vấn sát hơn, bạn cho mình biết dịp mặc, ngân sách và màu hoặc chất liệu bạn thích nhé.",
      "Bạn nói thêm giúp mình dịp mặc và gu màu sắc một chút, mình sẽ gợi ý chính xác hơn.",
      "Mình cần thêm vài chi tiết như dịp mặc, ngân sách hoặc chất liệu bạn thích để lọc mẫu đúng hơn.",
      "Cho mình xin thêm bối cảnh như đi dạy, đi tiệc hay chụp ảnh để mình tư vấn đúng hướng hơn nhé.",
      "Nếu bạn muốn mình chốt nhanh hơn, hãy cho mình biết màu bạn thích hoặc mức ngân sách dự kiến.",
      "Mình tư vấn tốt nhất khi biết dịp mặc và một vài ưu tiên như màu, chất liệu hay ngân sách của bạn.",
      "Bạn mô tả thêm một chút về nhu cầu hiện tại nhé, ví dụ dịp mặc hoặc kiểu bạn đang muốn tìm.",
      "Mình cần thêm thông tin để không gợi ý quá rộng. Bạn đang ưu tiên dịp mặc, màu hay mức giá nào hơn?",
      "Nói thêm cho mình vài ý như màu sắc, dịp mặc hoặc chất liệu bạn thích, mình sẽ lọc lại gọn hơn.",
      "Để tránh gợi ý lan man, bạn cho mình biết trước dịp mặc và tone màu bạn đang quan tâm nhé."
    ],
    ["out_of_scope"] =
    [
      "Mình đang tập trung hỗ trợ tư vấn áo dài, phối phụ kiện và thử đồ AI. Nếu muốn, mình có thể gợi ý mẫu phù hợp ngay cho bạn.",
      "Phần này mình chưa hỗ trợ trực tiếp, nhưng mình có thể giúp bạn chọn áo dài hoặc phối set theo nhu cầu hiện tại.",
      "Mình chuyên về tư vấn áo dài từ catalog và hỗ trợ thử đồ AI. Bạn muốn mình gợi ý mẫu theo dịp mặc không?",
      "Hiện mình hỗ trợ tốt nhất ở phần chọn mẫu, phối phụ kiện và chuẩn bị thử đồ AI. Bạn muốn bắt đầu từ dịp mặc nào?",
      "Mình chưa xử lý nội dung đó, nhưng mình có thể hỗ trợ ngay nếu bạn muốn tìm áo dài theo màu, dịp hoặc ngân sách.",
      "Nếu mục tiêu của bạn là chọn áo dài phù hợp, mình có thể vào việc ngay từ dịp mặc hoặc tone màu bạn thích.",
      "Mình chưa phụ trách phần này, nhưng vẫn có thể giúp bạn lọc mẫu áo dài và set phụ kiện theo nhu cầu thật nhanh.",
      "Phạm vi hỗ trợ của mình là tư vấn áo dài và thử đồ AI. Bạn muốn xem mẫu theo dịp hay gửi ảnh để mình hỗ trợ tiếp?",
      "Mình không xử lý phần đó, nhưng nếu bạn cần gợi ý áo dài đang có trong catalog thì mình hỗ trợ ngay được.",
      "Mình đang tập trung vào chuyện chọn mẫu và thử đồ. Bạn chỉ cần nói dịp mặc hoặc màu bạn thích là mình bắt đầu được ngay."
    ],
    ["catalog_lookup_empty"] =
    [
      "Mình chưa thấy mẫu live nào khớp sát mô tả này. Bạn thử nói rõ hơn màu, chất liệu hoặc dịp mặc để mình lọc lại nhé.",
      "Hiện mình chưa tìm được mẫu đang có sẵn khớp ngay. Bạn thêm giúp mình màu hoặc chất liệu bạn muốn để mình tìm chính xác hơn.",
      "Catalog hiện chưa ra kết quả đủ sát với mô tả này. Bạn thử nêu rõ dịp mặc hoặc mức ngân sách để mình lọc lại.",
      "Mình chưa thấy mẫu phù hợp ngay trong catalog live. Nếu bạn muốn, mình có thể tìm lại theo tone màu hoặc chất liệu cụ thể hơn.",
      "Chưa có mẫu nào khớp đủ gần với yêu cầu hiện tại. Bạn nói thêm một chút về màu hay dáng bạn thích nhé.",
      "Mình chưa lọc ra được mẫu đúng ý ngay lúc này. Bạn thử thu hẹp theo màu, dịp mặc hoặc ngân sách để mình tìm lại nhanh hơn.",
      "Kết quả hiện tại chưa có mẫu nào thật sự khớp. Bạn mô tả thêm giúp mình để mình refine lại bộ lọc.",
      "Tạm thời mình chưa thấy mẫu live phù hợp nhất. Bạn muốn mình lọc lại theo màu nổi bật, chất liệu hay hoàn cảnh mặc trước?",
      "Mình chưa tìm được mẫu đúng mô tả trong catalog hiện có. Cho mình thêm một tiêu chí nữa để mình lọc kỹ hơn nhé.",
      "Hiện chưa có kết quả đủ sát để mình tự tin gợi ý ngay. Bạn thử nói rõ hơn dịp mặc hoặc tone bạn đang muốn hướng tới."
    ],
    ["recommendation_empty"] =
    [
      "Mình chưa ghép được set thật phù hợp từ catalog hiện tại. Bạn thử nới ngân sách hoặc đổi màu, mình tìm lại ngay cho bạn.",
      "Hiện mình chưa chốt được set nào đủ hợp với các ràng buộc này. Nếu muốn, bạn có thể đổi tone màu hoặc mức giá một chút.",
      "Với các tiêu chí hiện tại, mình chưa ghép được set ưng ý từ catalog live. Bạn thử mở rộng thêm một chút để mình tìm lại nhé.",
      "Mình chưa có set nào thật sự ổn theo điều kiện bạn đưa ra. Bạn muốn ưu tiên giữ màu hay giữ ngân sách để mình cân lại?",
      "Catalog hiện chưa cho ra combo đủ sát với nhu cầu này. Bạn thử nới một tiêu chí để mình ghép lại set hợp hơn.",
      "Mình chưa tìm được set hoàn chỉnh phù hợp nhất lúc này. Nếu bạn đổi màu hoặc chất liệu, khả năng ra kết quả sẽ tốt hơn.",
      "Tạm thời mình chưa ghép được set hợp từ catalog hiện có. Bạn thử giảm bớt ràng buộc để mình lọc lại nhanh hơn nhé.",
      "Chưa có set nào đủ đẹp và đúng tiêu chí để mình đề xuất ngay. Bạn muốn mình ưu tiên dịp mặc hay tone màu trước?",
      "Mình chưa chốt được combo phù hợp với các điều kiện hiện tại. Nới nhẹ ngân sách hoặc đổi chất liệu có thể giúp ra mẫu đẹp hơn.",
      "Hiện chưa có set nào đủ khớp để mình tự tin đề xuất. Bạn nói mình giữ tiêu chí nào quan trọng nhất, mình sẽ tìm lại theo hướng đó."
    ],
    ["comparison_need_more_refs"] =
    [
      "Để so sánh rõ hơn, bạn chỉ giúp mình ít nhất hai mẫu nhé, ví dụ mẫu đầu tiên và mẫu thứ hai.",
      "Mình cần tối thiểu hai mẫu để đặt lên bàn cân. Bạn có thể nói rõ hai mẫu bạn muốn so sánh không?",
      "Bạn chọn giúp mình hai mẫu cụ thể nhé, mình mới so sánh được điểm hơn kém rõ ràng cho bạn.",
      "Để mình so sánh chính xác, bạn hãy chỉ ra ít nhất hai mẫu đang muốn cân nhắc.",
      "Mình cần biết rõ hai mẫu nào bạn muốn đặt cạnh nhau thì mới phân tích được sát nhu cầu.",
      "Bạn nhắn theo kiểu “so sánh mẫu 1 với mẫu 2” là mình có thể vào so sánh ngay.",
      "Cho mình hai mẫu cụ thể nhé, mình sẽ chỉ ra nhanh đâu là mẫu hợp hơn với nhu cầu của bạn.",
      "Mình chưa đủ điểm tham chiếu để so sánh. Bạn chọn trước hai mẫu, mình sẽ phân tích từng điểm cho bạn.",
      "Để phần so sánh có ích hơn, bạn giúp mình khoanh lại hai mẫu cụ thể nhé.",
      "Mình cần ít nhất hai mẫu để đối chiếu. Bạn chỉ rõ tên mẫu hoặc vị trí sản phẩm, mình sẽ so sánh ngay."
    ],
    ["comparison_insufficient_data"] =
    [
      "Mình chưa đủ dữ liệu để so sánh hai mẫu đó một cách rõ ràng.",
      "Hiện dữ liệu của hai mẫu này chưa đủ để mình đưa ra so sánh đáng tin cậy.",
      "Mình chưa có đủ thông tin cần thiết để đặt hai mẫu này lên so sánh cho bạn.",
      "Phần dữ liệu hiện tại chưa đủ để mình phân tích chênh lệch giữa hai mẫu này.",
      "Mình chưa đủ dữ kiện để so sánh chính xác hai mẫu bạn đang hỏi.",
      "Tạm thời mình chưa thể đối chiếu hai mẫu này vì dữ liệu chưa đầy đủ.",
      "Mình chưa có đủ thông tin để kết luận mẫu nào hơn trong cặp này.",
      "Hiện dữ liệu sản phẩm chưa đủ để mình so sánh rõ từng điểm giữa hai mẫu đó.",
      "Mình chưa đủ cơ sở để đưa ra phần so sánh thật hữu ích cho hai mẫu này.",
      "Mình cần thêm dữ liệu từ catalog để so sánh hai mẫu đó chính xác hơn."
    ],
    ["tryon_need_garment"] =
    [
      "Mình cần biết chính xác mẫu áo dài nào bạn muốn thử trước. Bạn có thể chọn trực tiếp trong thẻ sản phẩm hoặc nói “thử mẫu đầu tiên”.",
      "Để thử đồ đúng, mình cần chốt trước mẫu áo dài. Bạn chọn giúp mình một mẫu cụ thể nhé.",
      "Mình chưa biết bạn muốn thử mẫu nào. Bạn chỉ rõ sản phẩm giúp mình để mình chuẩn bị đúng.",
      "Phần thử đồ cần một mẫu áo dài cụ thể trước. Bạn chọn một mẫu rồi mình hỗ trợ tiếp ngay.",
      "Cho mình biết chính xác mẫu bạn muốn thử nhé, như vậy mình mới chuẩn bị đúng cho bước tiếp theo.",
      "Mình cần chốt sản phẩm trước khi thử đồ. Bạn có thể nói “thử cái đầu tiên” hoặc chọn trực tiếp trong danh sách.",
      "Để mình bật đúng bước thử đồ, bạn giúp mình chọn rõ mẫu áo dài đang muốn mặc thử nhé.",
      "Mình cần một mẫu cụ thể để chạy thử đồ. Bạn chọn trước sản phẩm, mình sẽ tiếp tục ngay.",
      "Hiện mình chưa xác định được mẫu cần thử. Bạn chỉ rõ một mẫu áo dài giúp mình nhé.",
      "Thử đồ sẽ chính xác hơn khi mình biết rõ mẫu bạn chọn. Bạn chốt trước một mẫu để mình xử lý tiếp nhé."
    ],
    ["tryon_need_person_image"] =
    [
      "Mình đã giữ sẵn mẫu bạn muốn thử. Bạn gửi thêm ảnh người mặc là mình có thể chuẩn bị bước tiếp theo ngay.",
      "Mẫu bạn chọn mình đã giữ rồi. Giờ chỉ cần một ảnh người mặc để bắt đầu thử đồ AI.",
      "Mình đã sẵn sàng cho bước thử đồ, chỉ còn thiếu ảnh người mặc của bạn thôi.",
      "Mẫu áo dài đã được chốt. Bạn gửi giúp mình ảnh người mặc để mình tiếp tục nhé.",
      "Mình đã giữ đúng mẫu rồi, giờ bạn chỉ cần tải lên ảnh người mặc là được.",
      "Mọi thứ gần như đã sẵn sàng, mình chỉ cần thêm ảnh người mặc để chạy thử đồ AI.",
      "Mình đã chuẩn bị xong phần mẫu thử. Bạn gửi ảnh người mặc để mình xử lý bước kế tiếp nhé.",
      "Mẫu đã được giữ sẵn cho bạn. Chỉ cần thêm ảnh người mặc là mình có thể bắt đầu thử ngay.",
      "Mình đã chốt đúng mẫu bạn muốn thử. Bạn gửi ảnh người mặc để mình tiếp tục nhé.",
      "Phần mẫu thì đã ổn rồi, bây giờ mình cần ảnh người mặc để tạo thử đồ AI cho bạn."
    ],
    ["tryon_ready"] =
    [
      "Mình đã sẵn sàng thử đồ với mẫu này. Bạn có thể bấm thử ngay hoặc thêm phụ kiện rồi chạy lại nếu muốn.",
      "Mẫu này đã sẵn cho bước thử đồ rồi. Nếu muốn, bạn có thể thử ngay hoặc chỉnh thêm phụ kiện trước.",
      "Mình đã chuẩn bị xong cho lần thử đồ này. Bạn thử ngay cũng được, hoặc thêm phụ kiện rồi mình chạy lại.",
      "Mọi thứ đã sẵn sàng để thử mẫu này. Bạn muốn thử ngay hay phối thêm phụ kiện trước?",
      "Mình đã chốt xong mẫu để thử. Bạn có thể bắt đầu ngay hoặc thêm một vài phụ kiện nếu muốn.",
      "Bước thử đồ đã sẵn sàng với mẫu này. Bạn cứ chạy thử ngay khi muốn nhé.",
      "Mình đã chuẩn bị xong cho mẫu này rồi. Nếu cần, bạn vẫn có thể thêm phụ kiện trước khi thử.",
      "Hiện mình đã sẵn sàng tạo thử đồ với mẫu bạn chọn. Bạn muốn thử luôn hay tinh chỉnh thêm?",
      "Mẫu này đã ở trạng thái sẵn sàng để thử. Bạn có thể bấm thử ngay trong khung chat.",
      "Phần chuẩn bị đã xong, bạn có thể thử ngay với mẫu này hoặc bổ sung phụ kiện rồi mình hỗ trợ tiếp."
    ],
    ["image_analysis_need_scenario"] =
    [
      "Mình đã nhận ảnh của bạn. Bạn cho mình biết đây là ảnh cho dịp nào để mình gợi ý sát hơn nhé.",
      "Ảnh mình đã nhận rồi. Nếu biết đây là đi dạy, đi tiệc, chụp ảnh hay lễ Tết thì mình sẽ tư vấn chuẩn hơn.",
      "Mình đã xem được ảnh rồi. Bạn nói thêm giúp mình bối cảnh mặc để mình gợi ý đúng hướng hơn nhé.",
      "Cho mình xin thêm dịp sử dụng của bức ảnh này, mình sẽ ưu tiên gợi ý đúng phong cách hơn.",
      "Mình đã nhận ảnh. Bạn đang muốn set đồ này cho đi dạy, đi tiệc, chụp ảnh hay dịp lễ Tết vậy?",
      "Ảnh đã có rồi, giờ chỉ cần biết dịp mặc để mình gợi ý chính xác và thực tế hơn.",
      "Mình đã nhận ảnh của bạn. Bạn cho mình biết đây là bối cảnh nào để mình chốt hướng tư vấn nhé.",
      "Mình xem được ảnh rồi. Nếu bạn nói thêm dịp mặc, mình sẽ chọn set hợp bối cảnh hơn nhiều.",
      "Mình đã có ảnh, nhưng cần thêm dịp sử dụng để không gợi ý sai tông cho bạn.",
      "Bạn nói thêm giúp mình đây là ảnh dùng cho hoàn cảnh nào nhé, mình sẽ dựa vào đó để gợi ý sát hơn."
    ],
    ["tryon_result"] =
    [
      "Mình đã tạo xong ảnh thử đồ mới trong cuộc trò chuyện. Nếu muốn, bạn có thể đổi mẫu hoặc thêm phụ kiện để thử lại tiếp.",
      "Ảnh thử đồ mới đã sẵn rồi. Nếu bạn muốn chỉnh thêm mẫu hoặc phụ kiện, mình có thể hỗ trợ tiếp ngay.",
      "Mình đã tạo xong phiên bản thử đồ mới cho bạn. Bạn muốn đổi sang mẫu khác hay thêm phụ kiện để chạy lại không?",
      "Kết quả thử đồ mới đã có trong chat rồi. Nếu cần, mình có thể giúp bạn thử lại với lựa chọn khác.",
      "Mình vừa tạo xong ảnh thử đồ. Nếu bạn muốn so lại nhiều mẫu, mình có thể tiếp tục hỗ trợ từng lựa chọn.",
      "Ảnh thử đồ đã được tạo xong. Bạn có thể xem trước rồi đổi mẫu hoặc thêm phụ kiện nếu muốn.",
      "Mình đã hoàn tất ảnh thử đồ mới. Nếu bạn muốn tinh chỉnh thêm, mình có thể chạy lại ngay.",
      "Kết quả thử đồ đã sẵn trong cuộc trò chuyện. Bạn muốn thử thêm phương án khác thì cứ nói mình nhé.",
      "Mình đã tạo xong ảnh thử đồ cho lần này. Nếu bạn muốn đổi mẫu hoặc phối thêm phụ kiện, mình hỗ trợ tiếp được ngay.",
      "Ảnh thử đồ mới đã có rồi. Bạn cứ xem trước, nếu muốn so thêm lựa chọn khác thì mình làm tiếp cho bạn."
    ],
    ["catalog_lookup_intro"] =
    [
      "Mình lọc ra được {count} mẫu khá hợp, bạn xem thử bên dưới nhé.",
      "Mình chọn nhanh {count} mẫu đang khá sát nhu cầu của bạn, kéo xuống là thấy ngay.",
      "Có {count} mẫu mình nghĩ bạn nên xem trước, mình để ngay bên dưới nhé.",
      "Mình gom lại {count} lựa chọn dễ cân nhắc nhất cho bạn rồi nè.",
      "Mình lọc sẵn {count} mẫu để bạn so nhanh, xem thử nhé."
    ],
    ["catalog_lookup_empty_for_type"] =
    [
      "Mình chưa thấy {productType} nào thật sự khớp với mô tả này trong catalog live. Bạn thử nói rõ hơn màu, chất liệu hoặc dịp mặc nhé.",
      "Hiện mình chưa lọc ra {productType} phù hợp từ catalog. Bạn thêm giúp mình một tiêu chí như màu hoặc chất liệu để mình tìm lại nhé.",
      "Catalog hiện chưa ra {productType} đủ sát với nhu cầu này. Bạn muốn mình lọc lại theo màu, chất liệu hay mức giá trước?",
      "Mình chưa tìm được {productType} đúng ý trong catalog hiện có. Bạn mô tả thêm một chút để mình refine lại nhé.",
      "Tạm thời mình chưa thấy {productType} nào khớp đủ gần. Bạn thử thu hẹp thêm theo tone màu, chất liệu hoặc dịp mặc nhé."
    ],
    ["accessory_recommendation_intro"] =
    [
      "Mình chọn trước vài phụ kiện hợp với gu bạn đang nói tới, bạn xem thử nhé.",
      "Mình gom vài món phụ kiện dễ phối với nhu cầu hiện tại của bạn ở dưới đây nha.",
      "Có vài phụ kiện đang khá ăn ý với kiểu bạn muốn, mình để sẵn bên dưới rồi.",
      "Mình chọn trước mấy món phụ kiện nhìn vào là phối được ngay, bạn xem thử nhé.",
      "Mình lọc giúp bạn vài phụ kiện hợp vibe hiện tại trước đã nha."
    ],
    ["product_description_intro"] =
    [
      "Mình tóm tắt nhanh các mẫu bạn đang hỏi ở dưới đây nhé:\n{description}",
      "Mình note nhanh đặc điểm từng mẫu để bạn dễ so hơn nhé:\n{description}",
      "Để mình tóm tắt gọn từng mẫu cho bạn dễ hình dung:\n{description}",
      "Mình viết ngắn gọn đặc điểm của từng mẫu ở đây nha:\n{description}",
      "Mình gom mô tả nhanh từng mẫu để bạn đỡ phải nhìn lại nhiều lần:\n{description}"
    ],
    ["comparison_result"] =
    [
      "{leftName} thiên về {leftRationale}, còn {rightName} thì {rightRationale}. Nếu cần thử trước, mình nghiêng về {suggestedName}.",
      "Nếu đặt cạnh nhau thì {leftName} sẽ nổi bật ở chỗ {leftRationale}, còn {rightName} hợp ở điểm {rightRationale}. Mình nghĩ bạn có thể thử {suggestedName} trước.",
      "Hai mẫu này đi hai hướng hơi khác nhau: {leftName} là kiểu {leftRationale}, còn {rightName} thì {rightRationale}. Muốn thử trước thì mình gợi ý {suggestedName}.",
      "{leftName} hợp khi bạn cần {leftRationale}, còn {rightName} lại mạnh ở {rightRationale}. Nếu chọn một mẫu để thử trước, mình sẽ chọn {suggestedName}.",
      "Mình thấy {leftName} và {rightName} khác nhau chủ yếu ở đây: một bên {leftRationale}, một bên {rightRationale}. Thử trước thì nên bắt đầu với {suggestedName}."
    ],
    ["image_analysis_acknowledged"] =
    [
      "Mình xem ảnh bạn gửi rồi, mình sẽ ưu tiên hướng hợp dịp {scenario} cho bạn nha.",
      "Mình thấy ảnh rồi, để mình bám theo bối cảnh {scenario} mà gợi ý cho sát hơn nhé.",
      "Ảnh mình nhận được rồi, mình sẽ ưu tiên set hợp dịp {scenario} và lên hình ổn hơn.",
      "Ok, mình xem ảnh rồi. Mình sẽ dựa trên dịp {scenario} để gợi ý cho hợp hơn nha.",
      "Mình đã xem ảnh, giờ mình bám theo bối cảnh {scenario} để chọn hướng phù hợp cho bạn."
    ],
    ["image_analysis_missing"] =
    [
      "Mình chưa thấy đúng ảnh bạn đang nhắc tới, bạn gửi lại giúp mình nha.",
      "Ảnh bạn đang nói tới hiện mình chưa lấy ra được, gửi lại giúp mình để mình xem kỹ hơn nhé.",
      "Mình chưa tìm được đúng ảnh đó trong cuộc trò chuyện, bạn gửi lại một lần nữa giúp mình nha.",
      "Có vẻ mình chưa bắt đúng ảnh bạn đang nhắc, bạn gửi lại để mình nhận xét chuẩn hơn nhé.",
      "Mình chưa mở được đúng ảnh đó, bạn gửi lại giúp mình để mình xem chính xác hơn nha."
    ],
    ["recommendation_exhausted"] =
    [
      "Mình đã đi gần hết các mẫu chưa trùng cho bối cảnh {scenario}. Nếu muốn, mình đổi hướng màu hoặc phối lại set cho bạn nhé.",
      "Các mẫu chưa lặp cho bối cảnh {scenario} mình gần như đã đi hết rồi. Mình có thể xoay sang hướng khác cho bạn.",
      "Với bối cảnh {scenario}, mình đã quét gần hết nhóm mẫu chưa trùng. Nếu bạn muốn, mình sẽ đổi gu một chút để mở thêm lựa chọn.",
      "Mình đã xem gần hết các mẫu chưa lặp cho dịp {scenario}. Muốn mình đổi tone hoặc đổi kiểu phối luôn không?",
      "Nhóm mẫu chưa trùng cho bối cảnh {scenario} mình đã đi gần hết. Nếu cần, mình sẽ mở sang hướng khác để bạn có thêm lựa chọn."
    ],
    ["recommendation_intro_plain"] =
    [
      "Mình lên sẵn vài look để bạn xem nhanh nhé:",
      "Mình phối thử vài look cho bạn đây:",
      "Mình chọn sẵn vài hướng phối để bạn dễ so nhé:",
      "Có vài look mình thấy khá ổn cho bạn, xem thử nha:",
      "Mình ghép sẵn vài look dễ mặc và dễ chọn cho bạn rồi đây:"
    ],
    ["recommendation_intro_scenario"] =
    [
      "Với bối cảnh {scenario}, mình lên sẵn vài look cho bạn nhé:",
      "Nếu đi theo hướng {scenario}, mình phối thử vài look thế này cho bạn:",
      "Mình chọn vài look hợp dịp {scenario} để bạn so nhanh nhé:",
      "Cho bối cảnh {scenario}, mình thấy mấy look này khá ổn:",
      "Nếu ưu tiên dịp {scenario}, mình sẽ bắt đầu từ vài look này nhé:"
    ],
    ["look_label_0"] = [ "Thanh lịch nổi bật", "Dễ mặc mà vẫn nổi", "An toàn nhưng vẫn có điểm nhấn" ],
    ["look_label_1"] = [ "Dịu mắt dễ mặc", "Mềm và nữ tính hơn", "Nhẹ nhàng, lên hình ổn" ],
    ["look_label_2"] = [ "Đổi gu mới hơn", "Có nét khác biệt hơn", "Lạ mắt hơn một chút" ],
    ["look_label_other"] = [ "Gợi ý thêm", "Một hướng khác", "Phương án khác" ],
    ["difference_reason_plain"] =
    [
      "tập trung vào áo dài chính để bạn dễ so nhanh giữa các mẫu.",
      "giữ trọng tâm ở áo dài để bạn nhìn ra khác biệt rõ hơn.",
      "đặt áo dài làm điểm chính để bạn cân giữa các mẫu dễ hơn."
    ],
    ["difference_reason_pair"] =
    [
      "mood khác nhờ đổi sang {garmentName} và ghép với {accessoryName}.",
      "không khí set thay đổi rõ hơn khi đi cùng {accessoryName} và {garmentName}.",
      "cảm giác tổng thể khác đi vì {garmentName} được phối với {accessoryName}."
    ],
    ["styling_tip_plain"] =
    [
      "Bạn có thể thêm phụ kiện sáng màu để tổng thể gọn và có điểm nhấn hơn.",
      "Nếu muốn set rõ điểm nhấn hơn, bạn thêm một món phụ kiện sáng màu là ổn.",
      "Chỉ cần thêm một phụ kiện sáng nhẹ là tổng thể sẽ cân hơn."
    ],
    ["styling_tip_pair"] =
    [
      "Tip phối: đi cùng {accessoryName} để set gọn mắt và đỡ bị rời tổng thể.",
      "Tip nhỏ: thêm {accessoryName} sẽ giúp tổng thể liền mạch hơn.",
      "Nếu phối với {accessoryName}, set này sẽ nhìn gọn và có điểm nhấn hơn."
    ]
  };

  public string Pick(string theme)
  {
    if (!Messages.TryGetValue(theme, out var variants) || variants.Count == 0)
    {
      throw new InvalidOperationException($"Unknown stylist fallback theme: {theme}");
    }

    return variants[Random.Shared.Next(variants.Count)];
  }

  public string Pick(string theme, params (string Key, string Value)[] placeholders)
  {
    var message = Pick(theme);
    if (placeholders.Length == 0)
    {
      return message;
    }

    foreach (var (key, value) in placeholders)
    {
      message = message.Replace($"{{{key}}}", value, StringComparison.Ordinal);
    }

    return message;
  }
}
