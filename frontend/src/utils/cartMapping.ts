import { resolveAssetUrl } from '../api/client';
import type { Cart } from '../types/cart';

export function normalizeCartAssets(cart: Cart): Cart {
  return {
    ...cart,
    items: cart.items.map((item) => ({
      ...item,
      imageUrl: resolveAssetUrl(item.imageUrl),
    })),
  };
}

export function emptyCartFrom<T extends Cart | null | undefined>(current: T): T | Cart {
  return current ? { ...current, items: [], subtotal: 0, totalItemCount: 0 } : current;
}
