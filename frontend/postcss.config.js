import postcssImport from 'postcss-import';
import postcssNested from 'postcss-nested';
import autoprefixer from 'autoprefixer';
import cssnano from 'cssnano';

export default {
  plugins: [
    postcssImport(),
    postcssNested(),
    autoprefixer(),
    cssnano({ preset: 'default' }),
  ],
};
