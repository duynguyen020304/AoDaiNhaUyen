import { motion } from 'framer-motion';
import { useState, useEffect } from 'react';
import styles from './DataDeletionPage.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

const sections = [
  { id: 'gioi-thieu', title: 'Giới thiệu' },
  { id: 'lua-chon', title: 'Lựa chọn phương án' },
  { id: 'ngat-ket-noi', title: 'Ngắt kết nối Zalo' },
  { id: 'xoa-toan-bo', title: 'Xóa toàn bộ dữ liệu' },
  { id: 'thoi-gian', title: 'Thời gian xử lý' },
  { id: 'du-lieu-xoa', title: 'Dữ liệu bị xóa' },
  { id: 'faq', title: 'Câu hỏi thường gặp' },
  { id: 'lien-he', title: 'Liên hệ hỗ trợ' },
];

export default function DataDeletionPage() {
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

  const emailSubject = encodeURIComponent('[YÊU CẦU XÓA DỮ LIỆU] - [Tên Zalo của bạn]');
  const emailBody = encodeURIComponent(`Chào Áo Dài Nhà Huyền,

Tôi là [Tên của bạn], email hoặc số điện thoại liên hệ [thông tin liên hệ của bạn], đã đăng nhập vào Áo Dài Nhà Huyền bằng Zalo.

Tôi viết email này để yêu cầu xóa hoàn toàn tài khoản và tất cả dữ liệu cá nhân của mình khỏi hệ thống của quý vị.

Thông tin xác nhận:
• Tên Zalo: [Tên của bạn]
• Email hoặc số điện thoại liên hệ: [thông tin liên hệ của bạn]
• Ngày yêu cầu: ${new Date().toLocaleDateString('vi-VN')}

Tôi xác nhận:
☑ Yêu cầu xóa toàn bộ dữ liệu
☑ Hiểu rằng hành động này không thể hoàn tác
☑ Đồng ý xóa lịch sử đơn hàng và thông tin giao hàng

Trân trọng,
[Tên đầy đủ của bạn]`);

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
            <h1>Xóa Dữ Liệu Người Dùng</h1>
            <p className={styles.subtitle}>
              Quyền của bạn đối với dữ liệu cá nhân
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

          {/* Section 1: Giới thiệu */}
          <motion.section id="gioi-thieu" className={styles.section} variants={fadeUp}>
            <h2>Quyền xóa dữ liệu của bạn</h2>
            <p>
              Tại Áo Dài Nhà Huyền, chúng tôi tôn trọng quyền của bạn đối với dữ liệu cá nhân. Bạn có quyền
              yêu cầu xóa hoàn toàn thông tin của mình khỏi hệ thống của chúng tôi bất cứ lúc nào.
            </p>
            <p className={styles.intro}>
              Trang này sẽ hướng dẫn bạn cách thực hiện yêu cầu xóa dữ liệu một cách đơn giản và minh bạch.
            </p>
          </motion.section>

          {/* Section 2: Hai cách để xóa/ngắt kết nối */}
          <motion.section id="lua-chon" className={styles.section} variants={fadeUp}>
            <h2>Lựa chọn phương án phù hợp với bạn</h2>
            <p>Bạn có hai lựa chọn để xử lý dữ liệu của mình:</p>

            <div className={styles.choiceGrid}>
              <div className={styles.choiceCard}>
                <div className={styles.choiceIcon}>1️⃣</div>
                <h3>Ngắt Kết Nối Zalo</h3>
                <p className={styles.choiceSubtext}>Giữ tài khoản, chỉ ngắt liên kết với Zalo</p>
                <p className={styles.choiceDesc}>
                  Phù hợp khi: Bạn muốn tiếp tục sử dụng dịch vụ Áo Dài Nhà Huyền nhưng không muốn liên kết với Zalo nữa.
                </p>
              </div>

              <div className={styles.choiceCard}>
                <div className={styles.choiceIcon}>2️⃣</div>
                <h3>Xóa Toàn Bộ Dữ Liệu</h3>
                <p className={styles.choiceSubtext}>Xóa hẳn tài khoản và mọi thông tin</p>
                <p className={styles.choiceDesc}>
                  Phù hợp khi: Bạn không còn muốn sử dụng dịch vụ và muốn xóa sạch tất cả dữ liệu cá nhân.
                </p>
              </div>
            </div>

            <p className={styles.note}>
              Vui lòng đọc kỹ từng phần dưới đây để chọn phương án phù hợp.
            </p>
          </motion.section>

          {/* Section 3: Hướng dẫn ngắt kết nối Zalo */}
          <motion.section id="ngat-ket-noi" className={styles.section} variants={fadeUp}>
            <h2>Cách 1 - Ngắt kết nối Zalo</h2>
            <p className={styles.sectionIntro}>
              Nếu bạn chỉ muốn ngắt liên kết giữa tài khoản Zalo và Áo Dài Nhà Huyền (vẫn giữ tài khoản trên hệ thống của chúng tôi), hãy làm theo các bước sau:
            </p>

            <div className={styles.methodBox}>
              <h3>📱 Trên trình duyệt web:</h3>
              <ol>
                <li>Truy cập: <a href="https://www.zalo.me" target="_blank" rel="noopener noreferrer">https://www.zalo.me</a></li>
                <li>Đăng nhập vào tài khoản Zalo của bạn</li>
                <li>Trong menu bên trái, chọn "Ứng dụng và trang web"</li>
                <li>Tìm ứng dụng "Áo Dài Nhà Huyền" trong danh sách</li>
                <li>Nhấp vào "Xóa" hoặc "Remove"</li>
              </ol>
            </div>

            <div className={styles.methodBox}>
              <h3>📱 Trên ứng dụng Zalo:</h3>
              <ol>
                <li>Mở ứng dụng Zalo</li>
                <li>Vào Menu (☰) → Cài đặt & quyền riêng tư → Cài đặt</li>
                <li>Cuộn xuống và chọn "Ứng dụng và trang web"</li>
                <li>Tìm "Áo Dài Nhà Huyền" và chọn "Xóa"</li>
              </ol>
            </div>

            <div className={styles.warningBox}>
              <h3>⚠️ Lưu ý quan trọng:</h3>
              <ul>
                <li>Ngắt kết nối Zalo sẽ <strong>không xóa</strong> tài khoản của bạn trên Áo Dài Nhà Huyền</li>
                <li>Lịch sử mua hàng và thông tin giao hàng vẫn được lưu trữ</li>
                <li>Bạn sẽ cần đăng ký lại bằng phương thức khác nếu muốn tiếp tục sử dụng</li>
                <li>Bạn vẫn có thể yêu cầu xóa toàn bộ dữ liệu bất cứ lúc nào (xem phần 2)</li>
              </ul>
              <p className={styles.warningNote}>
                Nếu bạn muốn xóa hoàn toàn tài khoản và dữ liệu, vui lòng làm theo hướng dẫn trong phần 2 dưới đây.
              </p>
            </div>
          </motion.section>

          {/* Section 4: Hướng dẫn xóa toàn bộ dữ liệu */}
          <motion.section id="xoa-toan-bo" className={styles.section} variants={fadeUp}>
            <h2>Cách 2 - Yêu cầu xóa toàn bộ dữ liệu</h2>
            <p className={styles.sectionIntro}>
              Để xóa hoàn toàn tài khoản và tất cả dữ liệu cá nhân của bạn khỏi hệ thống Áo Dài Nhà Huyền, vui lòng gửi yêu cầu qua email:
            </p>

            <div className={styles.emailBox}>
              <p><strong>📧 Gửi email tới:</strong> <a href="mailto:thavannanh24@gmail.com">thavannanh24@gmail.com</a></p>
              <p><strong>📌 Tiêu đề:</strong> [YÊU CẦU XÓA DỮ LIỆU] - [Tên Zalo của bạn]</p>
            </div>

            <div className={styles.emailTemplate}>
              <h3>📋 Nội dung email vui lòng bao gồm:</h3>

              <div className={styles.templateSection}>
                <h4>1. Thông tin cá nhân:</h4>
                <ul>
                  <li>Tên trên Zalo: __________________</li>
                  <li>Email hoặc số điện thoại liên hệ: __________________</li>
                  <li>Số điện thoại (nếu có): __________________</li>
                </ul>
              </div>

              <div className={styles.templateSection}>
                <h4>2. Yêu cầu cụ thể:</h4>
                <ul>
                  <li>☐ Tôi yêu cầu xóa toàn bộ tài khoản và dữ liệu cá nhân</li>
                  <li>☐ Tôi hiểu rằng việc này không thể hoàn tác</li>
                  <li>☐ Tôi đồng ý xóa toàn bộ lịch sử đơn hàng và thông tin giao hàng</li>
                </ul>
              </div>

              <div className={styles.templateSection}>
                <h4>3. Xác nhận thêm:</h4>
                <ul>
                  <li>Ngày gửi yêu cầu: __________________</li>
                  <li>Chữ ký (gõ tên đầy đủ của bạn): __________________</li>
                </ul>
              </div>

              <div className={styles.exampleBox}>
                <h4>Ví dụ email:</h4>
                <pre>
{`Tiêu đề: [YÊU CẦU XÓA DỮ LIỆU] - Nguyen Thi Lan

Nội dung:
Chào Áo Dài Nhà Huyền,

Tôi là Nguyen Thi Lan, email liên hệ lan.nguyen@email.com, đã đăng nhập vào Áo Dài Nhà Huyền bằng Zalo.

Tôi viết email này để yêu cầu xóa hoàn toàn tài khoản và tất cả dữ liệu cá nhân của mình khỏi hệ thống của quý vị.

Thông tin xác nhận:
• Tên Zalo: Nguyen Thi Lan
• Email hoặc số điện thoại liên hệ: lan.nguyen@email.com
• Ngày yêu cầu: 18/04/2026

Tôi xác nhận:
☑ Yêu cầu xóa toàn bộ dữ liệu
☑ Hiểu rằng hành động này không thể hoàn tác
☑ Đồng ý xóa lịch sử đơn hàng và thông tin giao hàng

Trân trọng,
Nguyen Thi Lan`}
                </pre>
              </div>
            </div>

            <div className={styles.quickAction}>
              <p>Hoặc gửi email nhanh bằng mẫu có sẵn:</p>
              <a
                href={`mailto:thavannanh24@gmail.com?subject=${emailSubject}&body=${emailBody}`}
                className={styles.emailButton}
              >
                📧 Gửi Email Yêu Cầu Xóa
              </a>
            </div>
          </motion.section>

          {/* Section 5: Thời gian xử lý */}
          <motion.section id="thoi-gian" className={styles.section} variants={fadeUp}>
            <h2>Thời gian và quy trình xử lý</h2>

            <div className={styles.timeline}>
              <div className={styles.timelineItem}>
                <div className={styles.timelineIcon}>⏱️</div>
                <div className={styles.timelineContent}>
                  <h3>Thời gian xử lý:</h3>
                  <ul>
                    <li>Chúng tôi sẽ xác nhận nhận được yêu cầu trong vòng: <strong>24 giờ</strong></li>
                    <li>Thời gian xử lý xóa dữ liệu: <strong>3-7 ngày làm việc</strong></li>
                    <li>Thời gian tối đa (trường hợp đặc biệt): <strong>30 ngày</strong></li>
                  </ul>
                </div>
              </div>
            </div>

            <div className={styles.process}>
              <h3>📋 Quy trình xử lý:</h3>

              <div className={styles.processStep}>
                <div className={styles.stepNumber}>1</div>
                <div className={styles.stepContent}>
                  <h4>Bước 1: Xác nhận yêu cầu (24 giờ)</h4>
                  <p>Chúng tôi sẽ gửi email xác nhận đã nhận yêu cầu của bạn. Nếu thông tin không đủ rõ, chúng tôi sẽ yêu cầu bổ sung.</p>
                </div>
              </div>

              <div className={styles.processStep}>
                <div className={styles.stepNumber}>2</div>
                <div className={styles.stepContent}>
                  <h4>Bước 2: Xác minh danh tính (1-2 ngày)</h4>
                  <p>Chúng tôi có thể gọi điện hoặc email để xác minh bạn là chủ tài khoản. Đây là bước bảo mật để ngăn chặn việc xóa dữ liệu sai mục đích.</p>
                </div>
              </div>

              <div className={styles.processStep}>
                <div className={styles.stepNumber}>3</div>
                <div className={styles.stepContent}>
                  <h4>Bước 3: Thực hiện xóa dữ liệu (3-5 ngày)</h4>
                  <ul>
                    <li>Xóa tài khoản khỏi hệ thống</li>
                    <li>Xóa thông tin cá nhân (tên, email, số điện thoại, địa chỉ)</li>
                    <li>Xóa thông tin đăng nhập và liên kết với Zalo</li>
                    <li>Ẩn danh hoặc xóa lịch sử giao dịch (tuỳ theo quy định pháp luật về lưu trữ hóa đơn)</li>
                  </ul>
                </div>
              </div>

              <div className={styles.processStep}>
                <div className={styles.stepNumber}>4</div>
                <div className={styles.stepContent}>
                  <h4>Bước 4: Thông báo hoàn tất (ngay sau khi xóa)</h4>
                  <ul>
                    <li>Gửi email xác nhận đã xóa xong</li>
                    <li>Thông tin về các dữ liệu đã bị xóa</li>
                    <li>Hướng dẫn nếu bạn muốn sử dụng dịch vụ lại trong tương lai</li>
                  </ul>
                </div>
              </div>
            </div>

            <div className={styles.legalNote}>
              <h3>⚠️ Lưu ý pháp lý:</h3>
              <p>
                Theo quy định pháp luật về thuế và kế toán, chúng tôi phải lưu giữ thông tin hóa đơn và giao dịch
                tối thiểu 5 năm. Dữ liệu này sẽ được lưu trữ an toàn và chỉ sử dụng cho mục đích pháp lý.
              </p>
            </div>
          </motion.section>

          {/* Section 6: Dữ liệu nào sẽ bị xóa */}
          <motion.section id="du-lieu-xoa" className={styles.section} variants={fadeUp}>
            <h2>Những gì sẽ bị xóa khi bạn yêu cầu</h2>

            <div className={styles.dataGrid}>
              <div className={styles.dataCard}>
                <h3>✅ SẼ BỊ XÓA:</h3>
                <ul>
                  <li>Tài khoản đăng nhập và thông tin xác thực</li>
                  <li>Tên, email, số điện thoại, địa chỉ</li>
                  <li>Ảnh đại diện và thông tin profile</li>
                  <li>Zalo ID và liên kết với Zalo</li>
                  <li>Thông tin sizing (chiều cao, cân nặng, số đo)</li>
                  <li>Lịch sử lướt web và tương tác</li>
                  <li>Giỏ hàng và danh sách yêu thích</li>
                  <li>Cài đặt tài khoản và quyền riêng tư</li>
                </ul>
              </div>

              <div className={`${styles.dataCard} ${styles.warning}`}>
                <h3>⚠️ KHÔNG XÓA NGAY (phải lưu theo pháp luật):</h3>
                <ul>
                  <li>Lịch sử đơn hàng và hóa đơn (lưu 5 năm theo luật thuế)</li>
                  <li>Thông tin giao hàng đã hoàn thành (lưu để bảo hành và khiếu nại)</li>
                  <li>Nhật ký hoạt động trong hệ thống (lưu để điều tra gian lận và bảo mật)</li>
                </ul>
                <p className={styles.dataNote}>
                  Dữ liệu được lưu theo pháp luật sẽ được:<br />
                  • Ẩn danh (không gắn với tên/email của bạn)<br />
                  • Lưu trữ an toàn và không sử dụng cho mục đích khác<br />
                  • Xóa vĩnh viễn sau khi hết thời hạn lưu trữ pháp lý
                </p>
              </div>
            </div>
          </motion.section>

          {/* Section 7: Câu hỏi thường gặp */}
          <motion.section id="faq" className={styles.section} variants={fadeUp}>
            <h2>Thắc mắc thường gặp</h2>

            <div className={styles.faqList}>
              <div className={styles.faqItem}>
                <h3>❓ Tôi có thể khôi phục lại dữ liệu sau khi xóa không?</h3>
                <p>
                  <strong>Không.</strong> Việc xóa dữ liệu là vĩnh viễn và không thể hoàn tác. Sau khi xóa, bạn sẽ
                  phải đăng ký tài khoản mới nếu muốn sử dụng dịch vụ lại.
                </p>
              </div>

              <div className={styles.faqItem}>
                <h3>❓ Tôi có thể xóa dữ liệu nhưng vẫn giữ đơn hàng đang giao không?</h3>
                <p>
                  <strong>Có.</strong> Vui lòng ghi rõ trong email yêu cầu: "Tôi muốn xóa tài khoản nhưng vẫn giữ đơn
                  hàng #[mã đơn hàng] đang giao". Chúng tôi sẽ hoàn thành đơn hàng trước khi xóa tài khoản.
                </p>
              </div>

              <div className={styles.faqItem}>
                <h3>❓ Nếu tôi chỉ muốn xóa một số thông tin nhất định thì sao?</h3>
                <p>
                  Vui lòng gửi email với tiêu đề "[CẬP NHẬT THÔNG TIN]" và liệt kê rõ bạn muốn xóa/chỉnh sửa thông
                  tin nào. Chúng tôi sẽ hỗ trợ bạn.
                </p>
              </div>

              <div className={styles.faqItem}>
                <h3>❓ Bao lâu thì dữ liệu thực sự biến mất khỏi hệ thống?</h3>
                <p>
                  Dữ liệu sẽ bị xóa ngay trong vòng 3-7 ngày làm việc sau khi xác minh danh tính. Tuy nhiên, có thể
                  có bản sao lưu (backup) cũ chưa được ghi đè. Chúng tôi đảm bảo dữ liệu này không thể truy cập được.
                </p>
              </div>

              <div className={styles.faqItem}>
                <h3>❓ Tôi có cần trả lại hàng đã mua không?</h3>
                <p>
                  <strong>Không.</strong> Yêu cầu xóa dữ liệu không ảnh hưởng đến quyền sở hữu sản phẩm bạn đã mua.
                </p>
              </div>

              <div className={styles.faqItem}>
                <h3>❓ Ai có thể nhìn thấy dữ liệu của tôi sau khi xóa?</h3>
                <p>
                  <strong>Không ai.</strong> Dữ liệu bị xóa hoàn toàn khỏi hệ thống. Chỉ có dữ liệu phải lưu theo
                  pháp luật (hóa đơn) được lưu ẩn danh.
                </p>
              </div>
            </div>
          </motion.section>

          {/* Section 8: Liên hệ hỗ trợ */}
          <motion.section id="lien-he" className={styles.section} variants={fadeUp}>
            <h2>Cần hỗ trợ thêm?</h2>
            <p>
              Nếu bạn gặp khó khăn khi gửi yêu cầu xóa dữ liệu hoặc có bất kỳ câu hỏi nào, vui lòng liên hệ:
            </p>

            <div className={styles.contactBox}>
              <p><strong>📧 Email:</strong> <a href="mailto:thavannanh24@gmail.com">thavannanh24@gmail.com</a></p>
              <p><strong>📌 Tiêu đề:</strong> [HỖ TRỢ] - [Vấn đề của bạn]</p>
            </div>

            <p>Chúng tôi cam kết phản hồi trong vòng 24-48 giờ làm việc.</p>
            <p className={styles.linkNote}>
              Nếu bạn chưa đọc Chính sách Quyền Riêng Tư, vui lòng truy cập:{' '}
              <a href="/privacy-policy">Trang Chính Sách Quyền Riêng Tư</a>
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
