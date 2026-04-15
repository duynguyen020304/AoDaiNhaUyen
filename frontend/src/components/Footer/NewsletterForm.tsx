import styles from './Footer.module.css';

export default function NewsletterForm() {
  return (
    <form className={styles.newsletterForm} onSubmit={(e) => e.preventDefault()}>
      <label className="sr-only" htmlFor="email">Email của bạn</label>
      <input id="email" type="email" placeholder="Email của bạn..." />
      <button className="hover-lift" type="submit">Gửi</button>
    </form>
  );
}
