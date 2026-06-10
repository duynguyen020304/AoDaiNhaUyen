import type { GetProductsParams } from '../api/catalog';

export const queryKeys = {
  auth: {
    me: ['auth', 'me'] as const,
  },
  categories: {
    header: ['categories', 'header'] as const,
  },
  products: {
    all: ['products'] as const,
    list: (params: GetProductsParams = {}) => ['products', 'list', params] as const,
    detail: (slug: string) => ['products', 'detail', slug] as const,
  },
  blog: {
    all: ['blog'] as const,
    list: (params: unknown = {}) => ['blog', 'list', params] as const,
    detail: (slug: string) => ['blog', 'detail', slug] as const,
    related: (slug: string) => ['blog', 'related', slug] as const,
    tags: ['blog', 'tags'] as const,
    categories: ['blog', 'categories'] as const,
  },
  cart: {
    current: ['cart'] as const,
  },
  addresses: {
    list: ['addresses', 'list'] as const,
  },
  orders: {
    list: ['orders', 'list'] as const,
  },
  user: {
    profile: ['user', 'profile'] as const,
  },
  media: {
    myImages: ['media', 'my-images'] as const,
  },
  chat: {
    threads: ['chat', 'threads'] as const,
    thread: (threadId: string) => ['chat', 'thread', threadId] as const,
  },
  aiTryOn: {
    catalog: (params: unknown = {}) => ['ai-tryon', 'catalog', params] as const,
  },
} as const;
