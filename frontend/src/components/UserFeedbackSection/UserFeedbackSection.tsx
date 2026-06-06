import { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import styles from './UserFeedbackSection.module.css';
import StarRating from '../StarRating/StarRating';
import { useAuth } from '../../auth/useAuth';
import { useAuthModal } from '../../auth/AuthModalContext';
import { getProductComments, createComment } from '../../api/comment';
import { staggerContainer, fadeUp, easeOutQuart } from '../../utils/motion';
import type { ReviewSummary, Comment } from '../../types/catalog';

/* ── helpers ── */

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('vi-VN', {
    day: 'numeric',
    month: 'numeric',
    year: 'numeric',
  });
}

function relativeTime(dateStr: string): string {
  const diffMs = Date.now() - new Date(dateStr).getTime();
  const diffMin = Math.floor(diffMs / 60000);
  const diffHr = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHr / 24);
  if (diffMin < 1) return 'Vừa xong';
  if (diffMin < 60) return `${diffMin} phút trước`;
  if (diffHr < 24) return `${diffHr} giờ trước`;
  if (diffDay < 30) return `${diffDay} ngày trước`;
  return formatDate(dateStr);
}

function Avatar({ name, url, size }: { name: string; url: string | null; size?: 'sm' }) {
  const sizeClass = size === 'sm' ? styles.avatarSm : '';
  if (url) {
    return <img className={`${styles.avatar} ${sizeClass}`} src={url} alt={name} loading="lazy" />;
  }
  return (
    <span className={`${styles.avatar} ${styles.avatarFallback} ${sizeClass}`} aria-hidden="true">
      {name.charAt(0).toUpperCase()}
    </span>
  );
}

function Skeleton() {
  return (
    <div className={styles.skeletonList}>
      {[1, 2, 3].map((i) => (
        <div key={i} className={styles.skeletonCard}>
          <div className={styles.skeletonCircle} />
          <div className={styles.skeletonBody}>
            <div className={styles.skeletonLine} style={{ width: '30%' }} />
            <div className={styles.skeletonLine} style={{ width: '90%' }} />
            <div className={styles.skeletonLine} style={{ width: '60%' }} />
          </div>
        </div>
      ))}
    </div>
  );
}

/* ── Unified comment form (text + optional stars) ── */

function UnifiedForm({
  isReply,
  onSubmit,
  onCancel,
}: {
  productId: string;
  isReply?: boolean;
  onSubmit: (content: string, rating?: number) => Promise<void>;
  onCancel?: () => void;
}) {
  const [text, setText] = useState('');
  const [rating, setRating] = useState(0);
  const [sending, setSending] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const max = 500;

  const canSend = text.trim().length > 0 && !sending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSend) return;
    setSending(true);
    setErr(null);
    try {
      await onSubmit(text.trim(), rating > 0 ? rating : undefined);
      setText('');
      setRating(0);
    } catch (ex: unknown) {
      setErr(ex instanceof Error ? ex.message : 'Có lỗi xảy ra.');
    } finally {
      setSending(false);
    }
  };

  return (
    <form className={styles.unifiedForm} onSubmit={handleSubmit} noValidate>
      <textarea
        className={styles.formTextarea}
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder={isReply ? 'Viết phản hồi...' : 'Chia sẻ cảm nhận của bạn...'}
        maxLength={max}
        rows={3}
        disabled={sending}
      />
      <div className={styles.formFooter}>
        {!isReply && (
          <StarRating rating={rating} size="md" interactive onRate={setRating} />
        )}
        <span className={`${styles.charCount} ${max - text.length < 20 ? styles.charWarn : ''}`}>
          {max - text.length}
        </span>
        <div className={styles.formActions}>
          {onCancel && (
            <button type="button" className={styles.cancelBtn} onClick={onCancel} disabled={sending}>
              Hủy
            </button>
          )}
          <button type="submit" className={styles.submitBtn} disabled={!canSend}>
            {sending ? 'Đang gửi...' : isReply ? 'Gửi' : rating > 0 ? 'Gửi đánh giá' : 'Gửi bình luận'}
          </button>
        </div>
      </div>
      {err && <p className={styles.formError}>{err}</p>}
    </form>
  );
}

/* ── props ── */

interface Props {
  productId: string;
  reviewSummary: ReviewSummary | null;
}

const PAGE = 10;

export default function UserFeedbackSection({ productId, reviewSummary }: Props) {
  const { user } = useAuth();
  const { openAuthModal } = useAuthModal();
  const isAuth = user != null;

  /* ── comments ── */
  const [comments, setComments] = useState<Comment[]>([]);
  const [comPage, setComPage] = useState(1);
  const [hasMoreCom, setHasMoreCom] = useState(false);
  const [loadingCom, setLoadingCom] = useState(true);
  const [comErr, setComErr] = useState<string | null>(null);
  const [replyingTo, setReplyingTo] = useState<string | null>(null);

  const fetchComments = useCallback(async (p: number, append = false) => {
    try { setComErr(null); if (!append) setLoadingCom(true);
      const r = await getProductComments(productId, p, PAGE);
      setComments(append ? (prev) => [...prev, ...r.data] : r.data);
      setHasMoreCom(r.hasNextPage); setComPage(p);
    } catch { setComErr('Không thể tải bình luận.'); }
    finally { setLoadingCom(false); }
  }, [productId]);
  useEffect(() => { fetchComments(1); }, [fetchComments]);

  const guard = () => { if (!isAuth) openAuthModal({ from: window.location.pathname }); return isAuth; };

  const handleReview = async (content: string, rating?: number) => {
    if (!guard()) return;
    await createComment(productId, content, { rating });
    fetchComments(1);
  };

  const handleReply = async (parentId: string, content: string) => {
    if (!guard()) return;
    await createComment(productId, content, { parentCommentId: parentId });
    fetchComments(1);
    setReplyingTo(null);
  };

  const dist = reviewSummary?.ratingDistribution ?? {};
  const distTotal = reviewSummary?.totalReviews ?? 0;

  return (
    <div className={styles.root}>
      {/* ── Rating summary (Shopee style) ── */}
      <h3 className={styles.sectionTitle}>ĐÁNH GIÁ SẢN PHẨM</h3>
      {reviewSummary && (
        <motion.div className={styles.summary} variants={fadeUp} initial="hidden" whileInView="show" viewport={{ once: true }}>
          <div className={styles.avgBlock}>
            <div className={styles.avgNumberRow}>
              <span className={styles.avgNumber}>{reviewSummary.averageRating.toFixed(1)}</span>
              <span className={styles.avgMax}>trên 5</span>
            </div>
            <StarRating rating={reviewSummary.averageRating} size="lg" />
          </div>
          <div className={styles.filterBlock}>
            <button className={`${styles.filterBtn} ${styles.filterBtnActive}`}>Tất Cả</button>
            <button className={styles.filterBtn}>5 Sao ({dist[5] ?? 0})</button>
            <button className={styles.filterBtn}>4 Sao ({dist[4] ?? 0})</button>
            <button className={styles.filterBtn}>3 Sao ({dist[3] ?? 0})</button>
            <button className={styles.filterBtn}>2 Sao ({dist[2] ?? 0})</button>
            <button className={styles.filterBtn}>1 Sao ({dist[1] ?? 0})</button>
            <button className={styles.filterBtn}>Có Bình Luận ({distTotal})</button>
          </div>
        </motion.div>
      )}

      {/* ── Unified form: star review + comment text ── */}
      {isAuth ? (
        <div className={styles.unifiedFormWrap}>
          <UnifiedForm productId={productId} onSubmit={handleReview} />
        </div>
      ) : (
        <div className={styles.actionRow}>
          <button type="button" className={styles.actionBtn} onClick={() => guard()}>
            ⭐ Đăng nhập để đánh giá & bình luận
          </button>
        </div>
      )}

      {/* ── Unified Reviews & Comments list ── */}
      <section className={styles.sectionBlock}>
        {loadingCom ? <Skeleton /> : null}
        {!loadingCom && comErr && <div className={styles.errorBlock}><p className={styles.errorText}>{comErr}</p><button className={styles.retryBtn} onClick={() => fetchComments(1)}>Thử lại</button></div>}
        {!loadingCom && !comErr && comments.length === 0 && <p className={styles.emptyText}>Chưa có đánh giá hay bình luận nào.</p>}
        {!loadingCom && !comErr && comments.length > 0 && (
          <motion.div className={styles.cardList} variants={staggerContainer} initial="hidden" whileInView="show" viewport={{ once: true, amount: 0.1 }}>
            {comments.map((c) => (
              <div key={c.id}>
                <motion.div className={styles.card} variants={fadeUp}>
                  <div className={styles.cardHeader}>
                    <Avatar name={c.userFullName} url={c.userAvatarUrl} />
                    <div className={styles.cardMeta}>
                      <span className={styles.userName}>{c.userFullName}</span>
                      <span className={styles.date}>{relativeTime(c.createdAt)}</span>
                    </div>
                  </div>
                  {c.rating && <StarRating rating={c.rating} size="sm" />}
                  <p className={styles.cardBody}>{c.content}</p>
                  <button type="button" className={styles.replyBtn} onClick={() => {
                    if (!guard()) return;
                    setReplyingTo((p) => (p === c.id ? null : c.id));
                  }}>Trả lời</button>
                  <AnimatePresence>
                    {replyingTo === c.id && (
                      <motion.div className={styles.replyFormWrap} initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }}>
                        <UnifiedForm productId={productId} isReply onSubmit={(txt) => handleReply(c.id, txt)} onCancel={() => setReplyingTo(null)} />
                      </motion.div>
                    )}
                  </AnimatePresence>
                </motion.div>
                {c.replies.length > 0 && (
                  <div className={styles.replies}>
                    <p className={styles.replyHeader}>Phản Hồi Từ Nhã Uyên</p>
                    {c.replies.map((r) => (
                      <motion.div key={r.id} className={styles.cardReplyInner} variants={fadeUp}>
                        <p className={styles.cardBody}>{r.content}</p>
                      </motion.div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </motion.div>
        )}
        {hasMoreCom && !loadingCom && <div className={styles.moreWrap}><button className={styles.moreBtn} onClick={() => fetchComments(comPage + 1, true)}>Xem thêm</button></div>}
      </section>
    </div>
  );
}
