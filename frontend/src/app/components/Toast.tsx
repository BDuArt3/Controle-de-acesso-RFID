import { useSyncExternalStore } from 'react';
import { CheckCircle2, AlertCircle, Info, X } from 'lucide-react';

interface Toast { id: string; message: string; type: 'success' | 'error' | 'info' }
let toasts: Toast[] = [];
const listeners = new Set<() => void>();
const notify = () => listeners.forEach(listener => listener());
export function removeToast(id: string) { toasts = toasts.filter(toast => toast.id !== id); notify(); }
export function showToast(message: string, type: Toast['type'] = 'info') {
  const id = crypto.randomUUID();
  toasts = [...toasts, { id, message, type }]; notify();
  setTimeout(() => removeToast(id), 4000);
}
export function ToastContainer() {
  const items = useSyncExternalStore(listener => { listeners.add(listener); return () => { listeners.delete(listener); }; }, () => toasts);
  return <div className="fixed right-4 top-4 z-[100] flex w-[calc(100%-2rem)] max-w-sm flex-col gap-3" aria-live="polite">{items.map(toast => {
    const Icon = toast.type === 'success' ? CheckCircle2 : toast.type === 'error' ? AlertCircle : Info;
    return <div key={toast.id} role={toast.type === 'error' ? 'alert' : 'status'} className="flex items-start gap-3 rounded-lg border border-border bg-card p-4 text-sm shadow-lg"><Icon className={`mt-0.5 size-5 shrink-0 ${toast.type === 'error' ? 'text-red-600' : toast.type === 'success' ? 'text-emerald-700' : 'text-muted-foreground'}`} /><p className="flex-1">{toast.message}</p><button aria-label="Fechar notificação" onClick={() => removeToast(toast.id)}><X className="size-4" /></button></div>;
  })}</div>;
}
