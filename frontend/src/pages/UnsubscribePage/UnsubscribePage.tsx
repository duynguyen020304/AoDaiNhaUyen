import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { confirmNewsletter, unsubscribeFromNewsletter } from '../../api/marketing';
import styles from './UnsubscribePage.module.css';

interface Props {
  mode: 'confirm' | 'unsubscribe';
}

export default function UnsubscribePage({ mode }: Props) {
  const [searchParams] = useSearchParams();
  const [message, setMessage] = useState('Đang xử lý yêu cầu...');
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const token = searchParams.get('token') ?? '';

  useEffect(() => {
    let cancelled = false;
    async function run() {
      if (!token) {
        setStatus('error');
        setMessage('Liên kết không hợp lệ hoặc thiếu token.');
        return;
      }

      try {
        const result = mode === 'confirm'
          ? await confirmNewsletter(token)
          : await unsubscribeFromNewsletter(token);
        if (!cancelled) {
          setStatus('success');
          setMessage(result.message);
        }
      } catch (error) {
        if (!cancelled) {
          setStatus('error');
          setMessage(error instanceof Error ? error.message : 'Không thể xử lý yêu cầu.');
        }
      }
    }

    void run();
    return () => {
      cancelled = true;
    };
  }, [mode, token]);

  return (
    <main className={styles.page}>
      <section className={styles.card} aria-live="polite">
        <p className={styles.kicker}>{mode === 'confirm' ? 'Xác nhận nhận tin' : 'Hủy nhận tin'}</p>
        <h1>{status === 'loading' ? 'Vui lòng chờ...' : status === 'success' ? 'Hoàn tất' : 'Có lỗi xảy ra'}</h1>
        <p>{message}</p>
        <Link className={styles.link} to="/">Về trang chủ</Link>
      </section>
    </main>
  );
}
