import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

const resources = {
  hu: {
    translation: {
      appTitle: 'Talált tárgy nyilvántartó',
      login: {
        title: 'Bejelentkezés',
        email: 'E-mail',
        password: 'Jelszó',
        submit: 'Belépés'
      },
      nav: {
        items: 'Tárgyak',
        storageLocations: 'Tárolási helyek',
        admin: 'Admin',
        users: 'Felhasználók',
        audit: 'Audit napló',
        itemsAudit: 'Talált tárgyak audit',
        currencies: 'Pénznemek és címletek',
        logout: 'Kijelentkezés'
      },
      items: {
        title: 'Talált tárgyak',
        create: 'Új tárgy rögzítése'
      }
    }
  },
  en: {
    translation: {
      appTitle: 'Lost and Found Registry',
      login: {
        title: 'Login',
        email: 'Email',
        password: 'Password',
        submit: 'Sign in'
      },
      nav: {
        items: 'Items',
        storageLocations: 'Storage locations',
        admin: 'Admin',
        users: 'Users',
        audit: 'Audit log',
        itemsAudit: 'Found items audit',
        currencies: 'Currencies & denominations',
        logout: 'Logout'
      },
      items: {
        title: 'Found items',
        create: 'Create item'
      }
    }
  }
};

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: 'hu',
    fallbackLng: 'en',
    interpolation: { escapeValue: false }
  });

export default i18n;
