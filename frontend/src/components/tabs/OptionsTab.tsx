import { WebSocketClient } from '@/services/websocket';
import { OptionsChain } from '@/components/trading/OptionsChain';
import { styles } from '@/styles/centralStyles';

interface OptionsTabProps {
  wsClient: WebSocketClient;
}

export function OptionsTab({ wsClient }: OptionsTabProps) {
  return (
    <div style={styles.content}>
      <OptionsChain wsClient={wsClient} />
    </div>
  );
}
