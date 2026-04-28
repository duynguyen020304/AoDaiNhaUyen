import { useEffect, useMemo, useRef, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import {
  createChatThread,
  executeChatTryOn,
  getChatThread,
  listChatThreads,
  streamChatMessage,
  type ChatMessage,
  type ChatRecommendationItem,
  type ChatStructuredPayload,
  type ChatThreadDetail,
  type ChatThreadSummary,
} from '../../api/chat';
import { resolveAssetUrl } from '../../api/client';
import { useToast } from '../Toast/useToast';
import styles from './ChatWidget.module.css';

const easeOutQuart = [0.22, 1, 0.36, 1] as const;
const TRY_ON_LOADING_INTENT = '__tryon_loading__';

export default function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [showThreads, setShowThreads] = useState(false);
  const [threads, setThreads] = useState<ChatThreadSummary[]>([]);
  const [activeThread, setActiveThread] = useState<ChatThreadDetail | null>(null);
  const [draft, setDraft] = useState('');
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [previewImage, setPreviewImage] = useState<{ url: string; name: string } | null>(null);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [queuePosition, setQueuePosition] = useState<number | null>(null);
  const [streamingMessage, setStreamingMessage] = useState<ChatMessage | null>(null);
  const [pendingTryOnByThread, setPendingTryOnByThread] = useState<Record<number, ChatMessage>>({});
  const [usedTryOnMessageByThread, setUsedTryOnMessageByThread] = useState<Record<number, number>>({});
  const { showToast } = useToast();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const endRef = useRef<HTMLDivElement | null>(null);
  const streamAbortControllerRef = useRef<AbortController | null>(null);

  const pendingFileEntries = useMemo(() => {
    return pendingFiles.map((file) => ({
      key: `${file.name}-${file.size}-${file.lastModified}`,
      name: file.name,
      url: URL.createObjectURL(file),
    }));
  }, [pendingFiles]);

  useEffect(() => {
    return () => {
      pendingFileEntries.forEach((entry) => URL.revokeObjectURL(entry.url));
    };
  }, [pendingFileEntries]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    let ignore = false;

    async function bootstrap() {
      setLoading(true);
      try {
        const summaries = await listChatThreads();
        if (ignore) {
          return;
        }

        setThreads(summaries);
        if (summaries.length > 0) {
          const thread = await getChatThread(summaries[0].id);
          if (!ignore) {
            setActiveThread(thread);
          }
        } else {
          const thread = await createChatThread();
          if (!ignore) {
            setActiveThread(thread);
            setThreads([{ id: thread.id, title: thread.title, preview: thread.messages.at(-1)?.content ?? null, status: thread.status, updatedAt: thread.updatedAt }]);
          }
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    bootstrap();

    return () => {
      ignore = true;
    };
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      endRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [activeThread, streamingMessage, isOpen]);

  useEffect(() => {
    return () => {
      streamAbortControllerRef.current?.abort();
    };
  }, []);

  const activeMessages = useMemo(() => {
    if (!activeThread) {
      return [] as ChatMessage[];
    }

    const pendingTryOnMessage = pendingTryOnByThread[activeThread.id];
    return pendingTryOnMessage
      ? [...activeThread.messages, pendingTryOnMessage]
      : activeThread.messages;
  }, [activeThread, pendingTryOnByThread]);

  const latestTryOnPayload = useMemo(() => {
    return [...activeMessages]
      .reverse()
      .find((message) => message.structuredPayload?.canTryOn);
  }, [activeMessages]);

  const isTryOnPending = activeThread ? Boolean(pendingTryOnByThread[activeThread.id]) : false;

  const updateThreadSummaryEntry = (threadId: number, preview: string | null, updatedAt: string) => {
    setThreads((current) => current.map((thread) => thread.id === threadId
      ? { ...thread, preview, updatedAt }
      : thread));
  };

  const addPendingTryOnMessage = (threadId: number, message: ChatMessage) => {
    setPendingTryOnByThread((current) => ({ ...current, [threadId]: message }));
  };

  const clearPendingTryOnMessage = (threadId: number) => {
    setPendingTryOnByThread((current) => {
      const next = { ...current };
      delete next[threadId];
      return next;
    });
  };

  const isTryOnLoadingMessage = (message: ChatMessage) => message.intent === TRY_ON_LOADING_INTENT;

  const renderMessages = activeMessages;

  const hasUsedLatestTryOn = activeThread && latestTryOnPayload
    ? usedTryOnMessageByThread[activeThread.id] === latestTryOnPayload.id
    : false;

  const canShowTryOnButton = latestTryOnPayload?.structuredPayload?.canTryOn && !isTryOnPending && !hasUsedLatestTryOn;

  const canSendMessage = !submitting;

  void canSendMessage;
  void canShowTryOnButton;
  void isTryOnLoadingMessage;
  void renderMessages;
  void clearPendingTryOnMessage;
  void addPendingTryOnMessage;
  void updateThreadSummaryEntry;
  void isTryOnPending;
  void latestTryOnPayload;
  void activeMessages;
  void hasUsedLatestTryOn;

  const handleCreateThread = async () => {
    streamAbortControllerRef.current?.abort();
    setStreamingMessage(null);
    setQueuePosition(null);
    setLoading(true);
    try {
      const thread = await createChatThread();
      setActiveThread(thread);
      setThreads((current) => [
        { id: thread.id, title: thread.title, preview: thread.messages.at(-1)?.content ?? null, status: thread.status, updatedAt: thread.updatedAt },
        ...current,
      ]);
      setShowThreads(false);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectThread = async (threadId: number) => {
    streamAbortControllerRef.current?.abort();
    setStreamingMessage(null);
    setQueuePosition(null);
    setLoading(true);
    try {
      setActiveThread(await getChatThread(threadId));
      setShowThreads(false);
    } finally {
      setLoading(false);
    }
  };

  const handleSend = async () => {
    if (!activeThread || (!draft.trim() && pendingFiles.length === 0)) {
      return;
    }

    const capturedMessage = draft.trim();
    const capturedFiles = [...pendingFiles];
    setSubmitting(true);

    const userMessage: ChatMessage = {
      id: -Date.now(),
      role: 'user',
      content: capturedMessage,
      intent: null,
      createdAt: new Date().toISOString(),
      attachments: capturedFiles.map((f, i) => ({
        id: -(i + 1),
        kind: 'user_image',
        fileUrl: URL.createObjectURL(f),
        mimeType: f.type,
        originalFileName: f.name,
        createdAt: new Date().toISOString(),
      })),
      structuredPayload: null,
    };

    setActiveThread((current) =>
      current ? { ...current, messages: [...current.messages, userMessage] } : current,
    );
    setDraft('');
    setPendingFiles([]);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    try {
      const streamAbortController = new AbortController();
      streamAbortControllerRef.current = streamAbortController;
      let assistantMessage: ChatMessage | null = null;
      let accumulatedText = '';
      let bufferedStructuredPayload: ChatStructuredPayload | null = null;

      for await (const event of streamChatMessage(activeThread.id, capturedMessage, capturedFiles, streamAbortController.signal)) {
        switch (event.type) {
          case 'queued': {
            setQueuePosition(event.data.position);
            break;
          }
          case 'created': {
            setQueuePosition(null);
            bufferedStructuredPayload = event.data.structuredPayload ?? null;
            assistantMessage = {
              id: event.data.messageId,
              role: 'assistant',
              content: '',
              intent: event.data.intent ?? null,
              createdAt: event.data.createdAt,
              attachments: event.data.attachments ?? [],
              structuredPayload: null,
            };
            setStreamingMessage(assistantMessage);
            break;
          }
          case 'text_delta': {
            accumulatedText += event.data.delta;
            setStreamingMessage((current) =>
              current ? { ...current, content: accumulatedText } : current,
            );
            break;
          }
          case 'text_done': {
            const finalMessage: ChatMessage = {
              id: event.data.messageId,
              role: 'assistant',
              content: event.data.fullText,
              intent: assistantMessage?.intent ?? null,
              createdAt: event.data.createdAt,
              attachments: assistantMessage?.attachments ?? [],
              structuredPayload: bufferedStructuredPayload,
            };
            setStreamingMessage(null);
            setActiveThread((current) => {
              if (!current) return current;
              const updated = { ...current, messages: [...current.messages, finalMessage], updatedAt: event.data.createdAt };
              setThreads((t) => updateThreadSummary(t, updated));
              return updated;
            });
            break;
          }
          case 'error': {
            setQueuePosition(null);
            setStreamingMessage(null);
            showToast(event.data.message, 'error');
            break;
          }
          case 'done': {
            setQueuePosition(null);
            break;
          }
        }
      }
    } catch (error) {
      setStreamingMessage(null);
      if (!(error instanceof DOMException && error.name === 'AbortError')) {
        showToast('Không thể nhận phản hồi trực tiếp từ stylist AI.', 'error');
      }
    } finally {
      setQueuePosition(null);
      streamAbortControllerRef.current = null;
      setSubmitting(false);
    }
  };

  const handleTryOn = async () => {
    if (!activeThread || !latestTryOnPayload?.structuredPayload?.selectedGarmentProductId) {
      return;
    }

    const threadId = activeThread.id;
    const sourceMessageId = latestTryOnPayload.id;
    const placeholderId = -Date.now();
    const placeholderMessage: ChatMessage = {
      id: placeholderId,
      role: 'assistant',
      content: 'Ảnh thử đồ của bạn đang được tạo, hãy chờ một chút nhé.',
      intent: TRY_ON_LOADING_INTENT,
      createdAt: new Date().toISOString(),
      attachments: [],
      structuredPayload: null,
    };

    setUsedTryOnMessageByThread((current) => ({ ...current, [threadId]: sourceMessageId }));
    addPendingTryOnMessage(threadId, placeholderMessage);
    updateThreadSummaryEntry(threadId, placeholderMessage.content, placeholderMessage.createdAt);

    try {
      const message = await executeChatTryOn(
        threadId,
        latestTryOnPayload.structuredPayload.selectedGarmentProductId,
        latestTryOnPayload.structuredPayload.selectedAccessoryProductIds,
      );
      clearPendingTryOnMessage(threadId);
      setActiveThread((current) => current?.id === threadId
        ? {
            ...current,
            messages: [...current.messages, message],
            updatedAt: message.createdAt,
          }
        : current);
      updateThreadSummaryEntry(threadId, message.content, message.createdAt);
    } catch {
      clearPendingTryOnMessage(threadId);
      setUsedTryOnMessageByThread((current) => {
        if (current[threadId] !== sourceMessageId) {
          return current;
        }

        const next = { ...current };
        delete next[threadId];
        return next;
      });
      const fallbackPreview = activeThread?.messages.at(-1)?.content ?? null;
      const fallbackUpdatedAt = activeThread?.updatedAt ?? placeholderMessage.createdAt;
      updateThreadSummaryEntry(threadId, fallbackPreview, fallbackUpdatedAt);
      showToast('Không thể tạo ảnh thử đồ lúc này.', 'error');
    }
  };

  return (
    <div className={styles.widgetRoot}>
      <AnimatePresence>
        {isOpen && (
          <motion.div
            className={styles.widgetWindow}
            initial={{ opacity: 0, scale: 0.85, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.85, y: 20 }}
            transition={{ duration: 0.32, ease: easeOutQuart }}
          >
            <div className={styles.conversation}>
              <header className={styles.conversationHeader}>
                <div className={styles.headerLeft}>
                  <button type="button" className={styles.menuButton} onClick={() => setShowThreads(true)}>
                    ☰
                  </button>
                  <div className={styles.conversationTitle}>{activeThread?.title ?? 'Stylist Chat'}</div>
                </div>
                <button type="button" className={styles.closeButton} onClick={() => setIsOpen(false)}>
                  ×
                </button>
              </header>

              <div className={styles.messages}>
                {renderMessages.map((message) => (
                  <MessageCard key={message.id} message={message} onPreviewImage={setPreviewImage} />
                ))}
                {queuePosition !== null ? (
                  <div className={styles.queueNotice}>
                    {queuePosition > 0
                      ? `Stylist AI đang bận, bạn đang ở vị trí ${queuePosition}. Vui lòng chờ một chút nhé.`
                      : 'Stylist AI đang chuẩn bị phản hồi, vui lòng chờ một chút nhé.'}
                  </div>
                ) : null}
                {streamingMessage && (
                  <MessageCard key="streaming" message={streamingMessage} onPreviewImage={setPreviewImage} streaming />
                )}
                <div ref={endRef} />
              </div>

              <div className={styles.composer}>
                {pendingFileEntries.length > 0 ? (
                  <div className={styles.pendingFiles}>
                    {pendingFileEntries.map((file) => (
                      <button
                        key={file.key}
                        type="button"
                        className={styles.pendingFile}
                        onClick={() => setPreviewImage({ url: file.url, name: file.name })}
                      >
                        {file.name}
                      </button>
                    ))}
                  </div>
                ) : null}
                <div className={styles.composerCard}>
                  <textarea
                    className={styles.composerInput}
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' && !event.shiftKey) {
                        event.preventDefault();
                        handleSend();
                      }
                    }}
                    placeholder="Gợi ý áo dài đi dạy màu nhã..."
                  />
                  <div className={styles.composerActions}>
                    <button type="button" className={styles.attachButton} onClick={() => fileInputRef.current?.click()}>
                      📎
                    </button>
                    <div className={styles.actionCluster}>
                      {canShowTryOnButton ? (
                        <button type="button" className={styles.tryOnButton} disabled={isTryOnPending} onClick={handleTryOn}>
                          Thử ngay
                        </button>
                      ) : null}
                      <button
                        type="button"
                        className={styles.sendButton}
                        disabled={submitting}
                        onClick={handleSend}
                        aria-label="Gửi tin nhắn"
                      >
                        <svg viewBox="0 0 24 24" aria-hidden="true" className={styles.sendIcon}>
                          <path d="M4.5 11.25 18.75 4.5l-3.375 14.25-4.5-5.25-6.375-2.25Z" />
                          <path d="m10.875 13.5 7.875-9" />
                        </svg>
                      </button>
                    </div>
                  </div>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    hidden
                    multiple
                    onChange={(event) => setPendingFiles(Array.from(event.target.files ?? []))}
                  />
                </div>
              </div>

              <AnimatePresence>
                {showThreads && (
                  <motion.div
                    className={styles.threadDrawer}
                    initial={{ x: '-100%' }}
                    animate={{ x: 0 }}
                    exit={{ x: '-100%' }}
                    transition={{ duration: 0.28, ease: easeOutQuart }}
                  >
                    <div className={styles.drawerHeader}>
                      <div className={styles.drawerTitle}>Cuộc trò chuyện</div>
                      <button type="button" className={styles.newThreadButton} onClick={handleCreateThread}>
                        + Mới
                      </button>
                    </div>
                    {loading && threads.length === 0 ? <div className={styles.loadingState}>Đang tải...</div> : null}
                    <div className={styles.threadList}>
                      {threads.map((thread) => (
                        <button
                          key={thread.id}
                          type="button"
                          className={`${styles.threadButton} ${activeThread?.id === thread.id ? styles.threadButtonActive : ''}`}
                          onClick={() => handleSelectThread(thread.id)}
                        >
                          <span className={styles.threadTitle}>{thread.title}</span>
                          <span className={styles.threadPreview}>{thread.preview ?? 'Chưa có tin nhắn'}</span>
                        </button>
                      ))}
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {previewImage ? (
          <motion.div
            className={styles.previewOverlay}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => setPreviewImage(null)}
          >
            <motion.img
              src={previewImage.url}
              alt={previewImage.name}
              className={styles.previewImage}
              initial={{ scale: 0.96, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.96, opacity: 0 }}
              onClick={(event) => event.stopPropagation()}
            />
            <button
              type="button"
              className={styles.previewClose}
              onClick={() => setPreviewImage(null)}
            >
              ×
            </button>
          </motion.div>
        ) : null}
      </AnimatePresence>

      <AnimatePresence>
        {!isOpen && (
          <motion.button
            type="button"
            className={styles.chatBubble}
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.8 }}
            transition={{ duration: 0.2 }}
            whileHover={{ scale: 1.04 }}
            whileTap={{ scale: 0.96 }}
            onClick={() => setIsOpen(true)}
          >
            <span className={styles.chatBubbleIcon}>✦</span>
            <span className={styles.chatBubbleLabel}>Tư vấn AI</span>
          </motion.button>
        )}
      </AnimatePresence>
    </div>
  );
}

function MessageCard(
  { message, onPreviewImage, streaming }: { message: ChatMessage; onPreviewImage: (preview: { url: string; name: string }) => void; streaming?: boolean },
) {
  const isUser = message.role === 'user';
  const isTryOnLoading = message.intent === TRY_ON_LOADING_INTENT;
  const productSections = getProductSections(message.structuredPayload);

  return (
    <div className={`${styles.messageRow} ${isUser ? styles.messageRowUser : ''}`}>
      <div className={styles.messageBubble}>
        <div className={streaming ? styles.streamingCursor : undefined}>{message.content}</div>
        {isTryOnLoading ? <div className={styles.tryOnSpinner} aria-label="Đang tạo ảnh thử đồ" /> : null}
        {message.attachments.map((attachment) => {
          const imageUrl = resolveAssetUrl(attachment.fileUrl) ?? attachment.fileUrl;
          const fileName = attachment.originalFileName ?? 'Xem ảnh đính kèm';

          if (attachment.mimeType.startsWith('image/')) {
            return (
              <button
                key={attachment.id}
                type="button"
                className={styles.attachmentLink}
                onClick={() => onPreviewImage({ url: imageUrl, name: fileName })}
              >
                <span className={styles.attachmentLinkIcon}>🖼</span>
                <span className={styles.attachmentLinkText}>{fileName}</span>
              </button>
            );
          }

          return null;
        })}
        {productSections.length ? (
          <div className={styles.productSections}>
            {productSections.map((section) => (
              <div key={section.key} className={styles.productSection}>
                <div className={styles.productSectionTitle}>{section.title}</div>
                <div className={styles.productGrid}>
                  {section.products.map((product) => (
                    <ProductCard key={`${section.key}-${product.productId}`} product={product} />
                  ))}
                </div>
              </div>
            ))}
          </div>
        ) : null}
      </div>
    </div>
  );
}

function ProductCard({ product }: { product: ChatRecommendationItem }) {
  return (
    <div className={styles.productCard}>
      {product.primaryImageUrl ? (
        <img
          className={styles.productImage}
          src={resolveAssetUrl(product.primaryImageUrl) ?? product.primaryImageUrl}
          alt={product.name}
        />
      ) : null}
      <div className={styles.productInfo}>
        <div className={styles.productName}>{product.name}</div>
        <div className={styles.productMeta}>
          {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(product.salePrice ?? product.price)}
        </div>
        <div className={styles.productRationale}>{product.rationale}</div>
      </div>
    </div>
  );
}

function getProductSections(payload: ChatStructuredPayload | null): Array<{ key: string; title: string; products: ChatRecommendationItem[] }> {
  if (!payload) {
    return [];
  }

  const garmentProducts = payload.garmentProducts?.length
    ? payload.garmentProducts
    : payload.products.filter((product) => product.productType === 'ao_dai');
  const accessoryProducts = payload.accessoryProducts?.length
    ? payload.accessoryProducts
    : payload.products.filter((product) => product.productType === 'phu_kien');

  const sections: Array<{ key: string; title: string; products: ChatRecommendationItem[] }> = [];

  if (garmentProducts.length) {
    sections.push({ key: 'garments', title: 'Áo dài gợi ý', products: garmentProducts });
  }

  if (accessoryProducts.length) {
    sections.push({ key: 'accessories', title: 'Phụ kiện đi kèm', products: accessoryProducts });
  }

  if (sections.length > 0) {
    return sections;
  }

  if (payload.products.length) {
    return [{ key: 'legacy', title: 'Sản phẩm gợi ý', products: payload.products }];
  }

  return [];
}

function updateThreadSummary(current: ChatThreadSummary[], thread: ChatThreadDetail): ChatThreadSummary[] {
  const next = [
    {
      id: thread.id,
      title: thread.title,
      preview: thread.messages.at(-1)?.content ?? null,
      status: thread.status,
      updatedAt: thread.updatedAt,
    },
    ...current.filter((item) => item.id !== thread.id),
  ];

  return next;
}
