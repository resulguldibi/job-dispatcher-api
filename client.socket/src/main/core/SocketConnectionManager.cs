using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client.socket.core
{
    public class SocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _sockets;
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public void AddSocket(WebSocket socket, SocketMessageConnectionInfo socketMessageConnectionInfo)
        {
            _sockets.TryAdd(socketMessageConnectionInfo.Id, socket);
        }

        public async Task RemoveSocket(string id, string message)
        {
            try
            {
                if (id != null && id != string.Empty && _sockets.TryRemove(id, out WebSocket socket) && socket != null && socket.State == WebSocketState.Open)
                {
                    message = $"Closed by the ConnectionManager with message :{message}";


                    await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                            statusDescription: message,
                                            cancellationToken: CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RemoveSocket Error : {0}", ex.Message);
            }
        }


        public async Task SendMessageToSocket(string id, string message)
        {

            _sockets.TryRemove(id, out WebSocket socket);


            message = $"Closed by the ConnectionManager with message :{message}";


            await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
                                                                  offset: 0,
                                                                  count: message.Length),
                                   messageType: WebSocketMessageType.Text,
                                   endOfMessage: true,
                                   cancellationToken: CancellationToken.None);
        }
    }
}
