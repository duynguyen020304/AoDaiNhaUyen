import { motion } from 'framer-motion';
import styles from './CartPage.module.css';
import CartItem from './CartItem';
import CustomerNotes from './CustomerNotes';
import CartSummary from './CartSummary';
import { cartItems } from './data';
import { fadeUp, sectionReveal } from '../../utils/motion';

export default function CartPage() {
  return (
    <motion.main
      className={styles.page}
      variants={sectionReveal}
      initial="hidden"
      animate="show"
    >
      <div className={styles.container}>
        <div className={styles.leftColumn}>
          {/* Cart Items Card */}
          <motion.div className={styles.card} variants={fadeUp}>
            <div className={styles.cardHeader}>
              <svg width="17.5" height="17.5" viewBox="0 0 17.5 17.5" fill="none" aria-hidden="true" role="img">
                <path d="M2.19 1.46h3.5l1.82 9.19a1.75 1.75 0 001.72 1.4h6.35a1.75 1.75 0 001.72-1.4L18.38 5.25H5.25" stroke="#0A0A0A" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round" />
                <circle cx="7" cy="15.75" r="1.17" stroke="#0A0A0A" strokeWidth="1.3" />
                <circle cx="15.75" cy="15.75" r="1.17" stroke="#0A0A0A" strokeWidth="1.3" />
              </svg>
              <span className={styles.cardHeaderText}>Giỏ hàng &nbsp;(1 items)</span>
            </div>
            <div className={styles.cardContent}>
              {cartItems.map((item) => (
                <div key={item.id}>
                  <CartItem item={item} />
                  <div className={styles.separator} />
                </div>
              ))}

            </div>
          </motion.div>

          {/* Customer Notes Card */}
          <motion.div variants={fadeUp}>
            <CustomerNotes />
          </motion.div>
        </div>

        <motion.div className={styles.rightColumn} variants={fadeUp}>
          <CartSummary />
        </motion.div>
      </div>

    </motion.main>
  );
}
