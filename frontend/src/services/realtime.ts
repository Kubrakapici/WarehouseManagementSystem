import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/stores/auth';

export function createStockHubConnection() {
  const auth = useAuthStore();
  const base = import.meta.env.VITE_API_URL?.trim() || '';
  const hubPath = '/hubs/stock';
  const hubUrl = base ? `${base.replace(/\/$/, '')}${hubPath}` : `${window.location.origin}${hubPath}`;

  const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: async () => auth.accessToken,
      skipNegotiation: false,
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect()
    .build();

  return connection;
}
