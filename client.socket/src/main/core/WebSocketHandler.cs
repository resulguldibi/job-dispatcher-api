using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace client.socket.core
{
    public abstract class WebSocketHandler
    {
        protected SocketConnectionManager SocketConnectionManager { get; set; }
        protected ILogger<WebSocketHandler> Logger;

        public WebSocketHandler(SocketConnectionManager webSocketConnectionManager, ILogger<WebSocketHandler> logger)
        {
            SocketConnectionManager = webSocketConnectionManager;
            Logger = logger;
        }

        public virtual async Task OnConnected(WebSocket socket, SocketMessageConnectionInfo socketMessageConnectionInfo)
        {
            await Task.Run(() => { SocketConnectionManager.AddSocket(socket, socketMessageConnectionInfo); });
        }

        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await SocketConnectionManager.RemoveSocket(SocketConnectionManager.GetId(socket), "client disconnected");
        }


        public virtual async Task RemoveSocket(string id,string message)
        {
            await SocketConnectionManager.RemoveSocket(id, message);
        }

        private async Task SendMessageAsync(WebSocket socket, string message)
        {                            
            await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
                                                                  offset: 0,
                                                                  count: message.Length),
                                   messageType: WebSocketMessageType.Text,
                                   endOfMessage: true,
                                   cancellationToken: CancellationToken.None);
        }

        public async Task SendMessageAsync(string id, string message)
        {
            var socket = SocketConnectionManager.GetSocketById(id);

            if (socket == null)
            {
                Logger.LogInformation("socket is null");
                return;
            }

            if (socket.State != WebSocketState.Open)
            {
                Logger.LogInformation("socket state is not open");
                await RemoveSocket(id, "socket removed because of state");
                Logger.LogInformation($"socket ({id}) removed because of state");
                return;
            }

            await SendMessageAsync(socket, message);
        }
        
        public abstract Task<SocketMessage> ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }

    [Serializable]
    public class SocketMessage
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }


    [Serializable]
    public class SocketMessageConnectionInfo
    {
        [JsonPropertyName("user")]
        public string User { get; set; }
        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        public string Id
        {
            get
            {
                return $"{this.User}";
            }
        }
    }
}