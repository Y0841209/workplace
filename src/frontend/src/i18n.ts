import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import esCO from './locales/es-CO.json';
import enUS from './locales/en-US.json';

const resources = {
  'es-CO': { translation: esCO },
  'en-US': { translation: enUS },
} as const;

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: 'es-CO',
    fallbackLng: 'en-US',
    interpolation: { escapeValue: false },
    detection: {
      order: ['querystring', 'cookie', 'localStorage', 'navigator', 'htmlTag'],
      caches: ['cookie'],
      lookupQuerystring: 'lng',
      lookupCookie: 'i18next',
      lookupLocalStorage: 'i18nextLng',
    },
    react: { useSuspense: false },
  });

export default i18n;