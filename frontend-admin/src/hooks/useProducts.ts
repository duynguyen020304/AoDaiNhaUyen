import { useReducer, useMemo, useState } from 'react'
import type { MockProduct } from '@/data/mockProducts'
import { initialProducts } from '@/data/mockProducts'

type Action =
  | { type: 'ADD'; product: MockProduct }
  | { type: 'UPDATE'; id: string; updates: Partial<MockProduct> }
  | { type: 'DELETE'; id: string }
  | { type: 'REORDER'; items: { id: string; sortOrder: number }[] }

function reducer(state: MockProduct[], action: Action): MockProduct[] {
  switch (action.type) {
    case 'ADD':
      return [...state, action.product]
    case 'UPDATE':
      return state.map(p => p.id === action.id ? { ...p, ...action.updates } : p)
    case 'DELETE':
      return state.filter(p => p.id !== action.id)
    case 'REORDER':
      return state.map(p => {
        const item = action.items.find(i => i.id === p.id)
        return item ? { ...p, sortOrder: item.sortOrder } : p
      })
    default:
      return state
  }
}

export function useProducts() {
  const [products, dispatch] = useReducer(reducer, initialProducts)
  const [filterType, setFilterType] = useState<string>('all')
  const [filterStatus, setFilterStatus] = useState<string>('all')
  const [search, setSearch] = useState('')

  const filteredProducts = useMemo(() => {
    let result = products
    if (filterType !== 'all') result = result.filter(p => p.type === filterType)
    if (filterStatus !== 'all') result = result.filter(p => p.status === filterStatus)
    if (search) {
      const q = search.toLowerCase()
      result = result.filter(p => p.name.toLowerCase().includes(q) || p.slug.toLowerCase().includes(q))
    }
    return result.sort((a, b) => a.sortOrder - b.sortOrder)
  }, [products, filterType, filterStatus, search])

  const getProduct = (id: string) => products.find(p => p.id === id)

  return {
    products: filteredProducts,
    allProducts: products,
    getProduct,
    addProduct: (p: MockProduct) => dispatch({ type: 'ADD', product: p }),
    updateProduct: (id: string, updates: Partial<MockProduct>) => dispatch({ type: 'UPDATE', id, updates }),
    deleteProduct: (id: string) => dispatch({ type: 'DELETE', id }),
    reorderProducts: (items: { id: string; sortOrder: number }[]) => dispatch({ type: 'REORDER', items }),
    filterType, setFilterType,
    filterStatus, setFilterStatus,
    search, setSearch,
  }
}
