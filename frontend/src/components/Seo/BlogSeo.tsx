import { Helmet } from 'react-helmet-async';

interface BlogSeoProps { title: string; description: string; canonical: string; image?: string | null; type?: 'website' | 'article'; robots?: string }
export function BlogSeo({ title, description, canonical, image, type = 'website', robots = 'index,follow' }: BlogSeoProps) {
  return (
    <Helmet>
      <title>{title}</title>
      <meta name="description" content={description} />
      <meta name="robots" content={robots} />
      <link rel="canonical" href={canonical} />
      <meta property="og:title" content={title} />
      <meta property="og:description" content={description} />
      <meta property="og:type" content={type} />
      <meta property="og:url" content={canonical} />
      <meta property="og:site_name" content="Áo Dài Nhã Uyên" />
      {image ? <meta property="og:image" content={image} /> : null}
      <meta name="twitter:card" content="summary_large_image" />
      <meta name="twitter:title" content={title} />
      <meta name="twitter:description" content={description} />
      {image ? <meta name="twitter:image" content={image} /> : null}
    </Helmet>
  );
}
