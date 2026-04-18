import { motion } from 'framer-motion';
import { useState, useEffect } from 'react';
import styles from './PrivacyPolicyPage.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

const sections = [
  { id: 'gioi-thieu', title: 'Giới thiệu' },
  { id: 'du-lieu-thu-thap', title: 'Dữ liệu thu thập' },
  { id: 'muc-dich-su-dung', title: 'Mục đích sử dụng' },
  { id: 'bao-mat', title: 'Bảo mật dữ liệu' },
  { id: 'chia-se', title: 'Chia sẻ dữ liệu' },
  { id: 'quyen-nguoi-dung', title: 'Quyền của bạn' },
  { id: 'facebook-login', title: 'Facebook Login' },
  { id: 'lien-he', title: 'Liên hệ' },
];

export default function PrivacyPolicyPage() {
  const [activeSection, setActiveSection] = useState('gioi-thieu');

  useEffect(() => {
    const handleScroll = () => {
      const sectionElements = sections.map(s => document.getElementById(s.id));
      const scrollPosition = window.scrollY + 150;

      for (let i = sectionElements.length - 1; i >= 0; i--) {
        const element = sectionElements[i];
        if (element && element.offsetTop <= scrollPosition) {
          setActiveSection(sections[i].id);
          break;
        }
      }
    };

    window.addEventListener('scroll', handleScroll);
    handleScroll();

    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const scrollToSection = (sectionId: string) => {
    const element = document.getElementById(sectionId);
    if (element) {
      const offsetTop = element.offsetTop - 100;
      window.scrollTo({
        top: offsetTop,
        behavior: 'smooth'
      });
    }
  };

  return (
    <main className={styles.page}>
        <motion.div
          className={styles.container}
          variants={staggerContainer}
          initial="hidden"
          animate="show"
        >
          {/* Header Section */}
          <motion.header className={styles.header} variants={fadeUp}>
            <h1>Chính sách Quyền Riêng Tư</h1>
            <p className={styles.lastUpdated}>
              Ngày có hiệu lực: 18 tháng 4 năm 2026
            </p>
          </motion.header>

          {/* Sidebar Navigation */}
          <motion.aside
            className={styles.sidebar}
            variants={fadeUp}
            initial="hidden"
            animate="show"
          >
            <nav>
              <h3>Nội dung</h3>
              <ul>
                {sections.map((section) => (
                  <li key={section.id}>
                    <button
                      onClick={() => scrollToSection(section.id)}
                      className={activeSection === section.id ? styles.active : ''}
                    >
                      {section.title}
                    </button>
                  </li>
                ))}
              </ul>
            </nav>
          </motion.aside>

          <div className={styles.contentWrapper}>

          {/* Section 1: Giới thiệu chung */}
          <motion.section id="gioi-thieu" className={styles.section} variants={fadeUp}>
            <h2>Chào mừng bạn đến với Áo Dài Nhà Huyền</h2>
            <p>
              Tại Áo Dài Nhà Huyền, chúng tôi trân trọng sự tin tưởng của bạn đối với thương hiệu của chúng tôi.
              Chính sách Quyền Riêng Tư này nhằm giải thích rõ ràng cách chúng tôi thu thập, sử dụng và bảo vệ
              thông tin cá nhân của bạn khi bạn sử dụng dịch vụ của chúng tôi, bao gồm cả việc đăng nhập
              thông qua tài khoản Facebook.
            </p>
            <p>Chúng tôi cam kết:</p>
            <ul>
              <li>Chỉ thu thập thông tin cần thiết để cung cấp dịch vụ tốt nhất</li>
              <li>Bảo mật an toàn cho mọi dữ liệu cá nhân của bạn</li>
              <li>Không chia sẻ thông tin của bạn với bên thứ ba mà không có sự đồng ý</li>
              <li>Cho phép bạn kiểm soát và quản lý dữ liệu của mình</li>
            </ul>
            <p className={styles.note}>
              Vui lòng đọc kỹ chính sách này để hiểu rõ hơn về quyền và nghĩa vụ của cả hai bên.
            </p>
          </motion.section>

          {/* Section 2: Dữ liệu chúng tôi thu thập */}
          <motion.section id="du-lieu-thu-thap" className={styles.section} variants={fadeUp}>
            <h2>Thông tin cá nhân chúng tôi thu thập</h2>
            <p>
              Khi bạn đăng nhập vào Áo Dài Nhà Huyền bằng Facebook, chúng tôi có thể thu thập các thông tin sau:
            </p>

            <div className={styles.subsection}>
              <h3>📌 Thông tin cơ bản từ Facebook:</h3>
              <ul>
                <li>Tên công khai của bạn trên Facebook</li>
                <li>Địa chỉ email liên kết với tài khoản Facebook</li>
                <li>Ảnh đại diện (profile picture) từ Facebook</li>
                <li>Facebook ID hoặc mã định danh tài khoản của bạn</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>📌 Thông tin bạn tự nguyện cung cấp:</h3>
              <ul>
                <li>Thông tin giao hàng (tên, số điện thoại, địa chỉ)</li>
                <li>Thông tin sizing cho áo dài (chiều cao, cân nặng, số đo cơ thể)</li>
                <li>Lịch sử đặt hàng và tương tác với dịch vụ</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>📌 Thông tin kỹ thuật:</h3>
              <ul>
                <li>Địa chỉ IP</li>
                <li>Loại thiết bị và trình duyệt bạn sử dụng</li>
                <li>Thời gian và ngày giờ truy cập</li>
              </ul>
            </div>

            <p className={styles.note}>
              Chúng tôi chỉ thu thập thông tin trong phạm vi cần thiết để cung cấp dịch vụ và cải thiện
              trải nghiệm của bạn.
            </p>
          </motion.section>

          {/* Section 3: Mục đích sử dụng dữ liệu */}
          <motion.section id="muc-dich-su-dung" className={styles.section} variants={fadeUp}>
            <h2>Chúng tôi sử dụng thông tin của bạn như thế nào</h2>
            <p>Áo Dài Nhà Huyền sử dụng dữ liệu cá nhân của bạn cho các mục đích sau:</p>

            <div className={styles.subsection}>
              <h3>✓ Cung cấp dịch vụ:</h3>
              <ul>
                <li>Xác thực tài khoản khi bạn đăng nhập</li>
                <li>Xử lý đơn đặt hàng áo dài và phụ kiện</li>
                <li>Giao hàng và theo dõi vận đơn</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>✓ Cá nhân hóa trải nghiệm:</h3>
              <ul>
                <li>Gợi ý sản phẩm phù hợp với sở thích của bạn</li>
                <li>Lưu thông tin sizing để đơn hàng sau nhanh hơn</li>
                <li>Gửi thông báo về trạng thái đơn hàng</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>✓ Cải thiện dịch vụ:</h3>
              <ul>
                <li>Phân tích hành vi sử dụng để nâng cao trải nghiệm người dùng</li>
                <li>Nghiên cứu và phát triển tính năng mới</li>
                <li>Đảm bảo hệ thống hoạt động ổn định</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>✓ Giao tiếp:</h3>
              <ul>
                <li>Gửi email xác nhận đơn hàng</li>
                <li>Thông báo về chương trình khuyến mãi (nếu bạn đồng ý nhận)</li>
                <li>Hỗ trợ khách hàng khi bạn cần trợ giúp</li>
              </ul>
            </div>

            <p className={styles.note}>
              Chúng tôi sẽ không sử dụng dữ liệu của bạn cho các mục đích khác mà không thông báo trước.
            </p>
          </motion.section>

          {/* Section 4: Lưu trữ và bảo mật dữ liệu */}
          <motion.section id="bao-mat" className={styles.section} variants={fadeUp}>
            <h2>Cách chúng tôi bảo vệ thông tin của bạn</h2>
            <p>Bảo mật dữ liệu là ưu tiên hàng đầu của Áo Dài Nhà Huyền:</p>

            <div className={styles.subsection}>
              <h3>🔒 Phương thức bảo mật:</h3>
              <ul>
                <li>Mật khẩu tài khoản được mã hóa one-way (không thể giải mã)</li>
                <li>Dữ liệu truyền qua kết nối HTTPS được mã hóa end-to-end</li>
                <li>Hệ thống được bảo vệ bằng tường lửa và các biện pháp bảo mật tiên tiến</li>
                <li>Quyền truy cập dữ liệu được giới hạn cho nhân viên có thẩm quyền</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>📅 Thời gian lưu trữ:</h3>
              <ul>
                <li>Thông tin tài khoản: Lưu trữ cho đến khi bạn yêu cầu xóa</li>
                <li>Lịch sử đơn hàng: Lưu trữ tối thiểu 5 năm theo quy định pháp luật</li>
                <li>Thông tin giao hàng: Lưu trữ để hỗ trợ khiếu nại và bảo hành</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>🔄 Backup dữ liệu:</h3>
              <p>
                Chúng tôi thực hiện sao lưu định kỳ để đảm bảo không mất mát dữ liệu. Bản sao lưu cũng được
                bảo mật với cùng tiêu chuẩn bảo vệ.
              </p>
            </div>

            <p className={styles.highlight}>
              Chúng tôi cam kết không bán, cho thuê hoặc thương mại hóa dữ liệu cá nhân của bạn dưới bất kỳ
              hình thức nào.
            </p>
          </motion.section>

          {/* Section 5: Chia sẻ dữ liệu với bên thứ ba */}
          <motion.section id="chia-se" className={styles.section} variants={fadeUp}>
            <h2>Khi nào chúng tôi chia sẻ thông tin</h2>
            <p>
              Áo Dài Nhà Huyền cam kết không chia sẻ thông tin cá nhân của bạn với bên thứ ba, ngoại trừ các
              trường hợp sau:
            </p>

            <div className={styles.subsection}>
              <h3>📦 Đối tác cung cấp dịch vụ:</h3>
              <ul>
                <li>Đơn vị vận chuyển (Giao Hàng Tiết Kiệm, GHN, Viettel Post...) để giao hàng</li>
                <li>Cổng thanh toán (nếu có) để xử lý thanh toán an toàn</li>
                <li>Đơn vị hosting và dịch vụ cloud để lưu trữ dữ liệu</li>
              </ul>
              <p className={styles.subNote}>
                Các đối tác này chỉ được tiếp cận thông tin cần thiết để hoàn thành dịch vụ và bị ràng buộc
                bởi nghĩa vụ bảo mật.
              </p>
            </div>

            <div className={styles.subsection}>
              <h3>⚖️ Theo yêu cầu pháp luật:</h3>
              <p>
                Chúng tôi có thể tiết lộ thông tin khi được yêu cầu bởi cơ quan chức năng có thẩm quyền theo
                quy định pháp luật, hoặc để bảo vệ quyền lợi hợp pháp của Áo Dài Nhà Huyền.
              </p>
            </div>

            <div className={styles.subsection}>
              <h3>🔄 Chuyển nhượng doanh nghiệp:</h3>
              <p>
                Trong trường hợp Áo Dài Nhà Huyền được bán hoặc sáp nhập với công ty khác, thông tin của bạn
                có thể là một phần của tài sản được chuyển nhượng. Chúng tôi sẽ thông báo trước cho bạn qua
                email.
              </p>
            </div>

            <p className={styles.note}>
              Ngoài các trường hợp trên, chúng tôi không chia sẻ dữ liệu của bạn mà không có sự đồng ý rõ ràng
              từ bạn.
            </p>
          </motion.section>

          {/* Section 6: Quyền của người dùng */}
          <motion.section id="quyen-nguoi-dung" className={styles.section} variants={fadeUp}>
            <h2>Quyền của bạn đối với dữ liệu cá nhân</h2>
            <p>
              Theo quy định pháp luật về bảo vệ dữ liệu cá nhân, bạn có các quyền sau:
            </p>

            <div className={styles.rightsList}>
              <div className={styles.rightItem}>
                <h3>👤 Quyền truy cập</h3>
                <p>Bạn có quyền yêu cầu xem bản sao toàn bộ dữ liệu cá nhân mà chúng tôi lưu trữ về bạn.</p>
              </div>

              <div className={styles.rightItem}>
                <h3>✏️ Quyền chỉnh sửa</h3>
                <p>Bạn có thể cập nhật, chỉnh sửa thông tin cá nhân của mình bất cứ lúc nào thông qua tài khoản hoặc email cho chúng tôi.</p>
              </div>

              <div className={styles.rightItem}>
                <h3>🗑️ Quyền xóa</h3>
                <p>Bạn có quyền yêu cầu xóa toàn bộ dữ liệu cá nhân của mình khỏi hệ thống Áo Dài Nhà Huyền.</p>
              </div>

              <div className={styles.rightItem}>
                <h3>🚫 Quyền hạn chế</h3>
                <p>Bạn có quyền yêu cầu giới hạn việc xử lý dữ liệu của mình trong một số trường hợp nhất định.</p>
              </div>

              <div className={styles.rightItem}>
                <h3>📧 Quyền phản đối</h3>
                <p>Bạn có quyền phản đối việc xử lý dữ liệu của mình cho mục đích marketing.</p>
              </div>

              <div className={styles.rightItem}>
                <h3>📦 Quyền chuyển dữ liệu</h3>
                <p>Bạn có quyền yêu cầu nhận dữ liệu của mình ở định dạng có cấu trúc để chuyển sang dịch vụ khác.</p>
              </div>

              <div className={styles.rightItem}>
                <h3>⛔ Quyền rút lại đồng ý</h3>
                <p>Bạn có quyền rút lại đồng ý của mình bất cứ lúc nào. Việc này không ảnh hưởng đến tính hợp pháp của việc xử lý dữ liệu trước khi bạn rút lại đồng ý.</p>
              </div>
            </div>

            <p className={styles.contactNote}>
              Để thực hiện các quyền này, vui lòng liên hệ với chúng tôi qua email:{' '}
              <a href="mailto:thavannanh24@gmail.com">thavannanh24@gmail.com</a>
            </p>
          </motion.section>

          {/* Section 7: Chính sách Facebook Login */}
          <motion.section id="facebook-login" className={styles.section} variants={fadeUp}>
            <h2>Quy định khi đăng nhập bằng Facebook</h2>
            <p>
              Khi bạn chọn đăng nhập vào Áo Dài Nhà Huyền bằng tài khoản Facebook, các điều sau được áp dụng:
            </p>

            <div className={styles.subsection}>
              <h3>🔗 Mối quan hệ với Facebook:</h3>
              <ul>
                <li>Áo Dài Nhà Huyền là một ứng dụng bên thứ ba sử dụng Facebook Login</li>
                <li>Facebook chỉ cung cấp thông tin mà bạn đã đồng ý chia sẻ</li>
                <li>Chúng tôi tuân thủ Chính sách Sử dụng Dữ liệu của Facebook</li>
              </ul>
            </div>

            <div className={styles.subsection}>
              <h3>📊 Dữ liệu chúng tôi nhận từ Facebook:</h3>
              <p>
                Chúng tôi chỉ yêu cầu và lưu trữ những thông tin cần thiết để tạo tài khoản và cung cấp dịch
                vụ. Chúng tôi không yêu cầu quyền truy cập bạn bè của bạn hay thông tin nhắn cảm xúc.
              </p>
            </div>

            <div className={styles.subsection}>
              <h3>🔌 Ngắt kết nối Facebook:</h3>
              <p>Bạn có thể ngắt kết nối tài khoản Facebook của mình với Áo Dài Nhà Huyền bất cứ lúc nào bằng cách:</p>
              <ol>
                <li>Vào Cài đặt Facebook</li>
                <li>Chọn "Ứng dụng và trang web"</li>
                <li>Tìm Áo Dài Nhà Huyền và chọn "Xóa"</li>
              </ol>
              <p className={styles.warning}>
                <strong>Lưu ý:</strong> Ngắt kết nối Facebook không tự động xóa tài khoản và dữ liệu của bạn
                trên Áo Dài Nhà Huyền. Nếu bạn muốn xóa hoàn toàn, vui lòng gửi yêu cầu theo hướng dẫn trong trang
                {' '}<a href="/data-deletion">"Xóa Dữ Liệu Người Dùng"</a>.
              </p>
            </div>

            <p className={styles.note}>
              Vui lòng xem Chính sách Quyền Riêng Tư của Facebook để hiểu rõ hơn về cách Facebook xử lý dữ
              liệu của bạn.
            </p>
          </motion.section>

          {/* Section 8: Liên hệ và hỗ trợ */}
          <motion.section id="lien-he" className={styles.section} variants={fadeUp}>
            <h2>Cách thức liên hệ với chúng tôi</h2>
            <p>
              Nếu bạn có bất kỳ câu hỏi, thắc mắc hoặc yêu cầu liên quan đến quyền riêng tư và dữ liệu cá
              nhân, vui lòng liên hệ:
            </p>

            <div className={styles.contactBox}>
              <p><strong>📧 Email:</strong> <a href="mailto:thavannanh24@gmail.com">thavannanh24@gmail.com</a></p>
              <p><strong>📌 Tiêu đề:</strong> [Quyền Riêng Tư] - [Tên yêu cầu của bạn]</p>
            </div>

            <p>Chúng tôi cam kết phản hồi trong vòng 24-48 giờ làm việc.</p>
            <p className={styles.linkNote}>
              Để xem hướng dẫn xóa dữ liệu, vui lòng truy cập:{' '}
              <a href="/data-deletion">Trang Xóa Dữ Liệu Người Dùng</a>
            </p>
          </motion.section>

          {/* Footer */}
          <motion.footer className={styles.footer} variants={fadeUp}>
            <p>© 2026 Áo Dài Nhà Huyền | Liên hệ: <a href="mailto:thavannanh24@gmail.com">thavannanh24@gmail.com</a></p>
            <div className={styles.footerLinks}>
              <a href="/privacy-policy">Chính sách Quyền Riêng Tư</a>
              <span> | </span>
              <a href="/data-deletion">Xóa Dữ Liệu Người Dùng</a>
            </div>
          </motion.footer>
          </div>
        </motion.div>
      </main>
  );
}
