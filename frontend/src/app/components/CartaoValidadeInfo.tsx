import { Badge } from './ui/badge';
import { getCartaoValidade } from '../services/cartaoValidade';
import type { CartaoDto } from '../services/api';

interface CartaoValidadeInfoProps {
  card: Pick<CartaoDto, 'dataExpiracao' | 'status'>;
  now?: Date;
}

export default function CartaoValidadeInfo({ card, now }: CartaoValidadeInfoProps) {
  const validade = getCartaoValidade(card, now);

  if (validade.estado === 'sem-validade') {
    return <span className="text-muted-foreground">—</span>;
  }
  if (validade.estado === 'invalida') {
    return <span className="text-muted-foreground">{validade.texto}</span>;
  }

  const vencida = validade.estado === 'vencida';
  return (
    <div className="space-y-1">
      <p className="whitespace-nowrap">{validade.texto}</p>
      <Badge
        variant={vencida ? 'destructive' : 'default'}
        className={vencida ? '' : 'bg-green-100 text-green-800'}
      >
        {vencida ? 'Vencida' : 'Vigente'}
      </Badge>
      {validade.bloqueioPendente && (
        <p className="max-w-56 whitespace-normal text-xs text-amber-700">
          Cartão vencido. O bloqueio será registrado na próxima leitura.
        </p>
      )}
    </div>
  );
}
