import { Shield } from 'lucide-react';
import { Card, CardContent } from '../ui/card';

export function AccessDeniedView() {
  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="p-8 text-center">
          <Shield className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
          <h2 className="text-2xl font-bold mb-2">Acesso Restrito</h2>
          <p className="text-muted-foreground">
            Você não tem permissão para acessar esta seção.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}