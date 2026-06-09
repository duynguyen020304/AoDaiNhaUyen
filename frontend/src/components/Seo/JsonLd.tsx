import { Helmet } from 'react-helmet-async';
export function JsonLd({ data }: { data: unknown }) {
  return <Helmet><script type="application/ld+json">{JSON.stringify(data)}</script></Helmet>;
}
