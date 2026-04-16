import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import styles from './NotFoundPage.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

export default function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <main className={styles.page}>
      <motion.div
        className={styles.content}
        variants={staggerContainer}
        initial="hidden"
        animate="show"
      >
        <motion.span className={styles.code} variants={fadeUp}>404</motion.span>
        <motion.h1 className={styles.title} variants={fadeUp}>Trang không tồn tại</motion.h1>
        <motion.p className={styles.desc} variants={fadeUp}>
          Xin lỗi, trang bạn đang tìm kiếm không tồn tại hoặc đã được di chuyển.
        </motion.p>
        <motion.a
          className={styles.cta}
          href="/"
          onClick={(e) => { e.preventDefault(); navigate('/'); }}
          variants={fadeUp}
          whileHover={{ y: -2, scale: 1.02 }}
          whileTap={{ scale: 0.97 }}
        >
          Về trang chủ <span aria-hidden="true">&rarr;</span>
        </motion.a>
      </motion.div>
    </main>
  );
}
