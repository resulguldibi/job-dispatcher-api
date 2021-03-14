using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.socket.core
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketHandler _webSocketHandler;

        public WebSocketManagerMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();

            await Receive(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    SocketMessage message = await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                    if ((bool)message?.Code?.Equals("connect"))
                    {
                        SocketMessageConnectionInfo socketMessageConnectionInfo = message.Data.ToJSON().FromJSON<SocketMessageConnectionInfo>();
                        await _webSocketHandler.OnConnected(socket, socketMessageConnectionInfo);

                        SocketMessage connectionResultMessage = new SocketMessage()
                        {
                            Code = "connected",
                            Data = "connected successfully"
                        };

                        await _webSocketHandler.SendMessageAsync(socketMessageConnectionInfo.Id, connectionResultMessage.ToJSON());
                    }

                    return;
                }

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocketHandler.OnDisconnected(socket);
                    return;
                }
            });
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                       cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);
            }
        }
    }
}