const { VITE_API_BASE_URL } = import.meta.env;

if (!VITE_API_BASE_URL) {
  throw new Error('Defina VITE_API_BASE_URL para o ambiente selecionado.');
}

export const API_BASE_URL = VITE_API_BASE_URL;
