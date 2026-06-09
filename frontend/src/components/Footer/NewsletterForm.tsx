import type { FormEvent } from 'react';
import { useState } from 'react';
import { subscribeToNewsletter } from '../../api/marketing';
import styles from './Footer.module.css';

export default function NewsletterForm() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setError(null);
    setIsSubmitting(true);

    try {
      const result = await subscribeToNewsletter(email);
      setMessage(result.message || 'Vui lòng kiểm tra email để xác nhận đăng ký.');
      setEmail('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể đăng ký nhận tin. Vui lòng thử lại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <>
      <form className={styles.newsletterForm} onSubmit={handleSubmit}>
        <label className="sr-only" htmlFor="email">Email của bạn</label>
        <input
          id="email"
          type="email"
          placeholder="Email của bạn..."
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
        <button className="hover-lift" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Đang gửi...' : 'Gửi'}
        </button>
      </form>
      {message ? <p aria-live="polite">{message}</p> : null}
      {error ? <p role="alert">{error}</p> : null}
    </>
  );
}
