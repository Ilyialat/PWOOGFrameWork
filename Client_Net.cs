using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace PWOOGFrameWork
{
    public partial class PWClient
    {
        private static byte[] GetArray(byte[] bt, int position, int count = 0)
        {
            if (count <= 0)
                count = bt.Length - position;
            byte[] result = new byte[count];
            for (int i = position; i < position + count; i++)
                result[i - position] = bt[i];
            return result;
        }

        private Socket socket;
        private SocketAsyncEventArgs socketRAEA;
        private SocketAsyncEventArgs socketSAEA;

        private PWCrypt crypt;

        private BlockingCollection<p_SPacket> toSend;
        private ActionQueueAsync sendQueue;
        private bool isSend;

        private PWClient()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveBufferSize = ushort.MaxValue; //Ничего не пропустим :D

            socketRAEA = new SocketAsyncEventArgs();
            socketRAEA.SetBuffer(new byte[ushort.MaxValue], 0, ushort.MaxValue);
            socketRAEA.Completed += new EventHandler<SocketAsyncEventArgs>(socket_Receive);

            socketSAEA = new SocketAsyncEventArgs();
            socketSAEA.Completed += new EventHandler<SocketAsyncEventArgs>(socket_Send);

            toSend = new BlockingCollection<p_SPacket>(20);
            sendQueue = new ActionQueueAsync(false);

            p_Inicialization(); //Client_Net_Packets
        }

        private void sendAsync(p_SPacket packet, bool t = false)
        {
            Action d = () =>
            {
                byte[] buff;
                if (!packet.IsToContainer)
                    buff = packet.Packet;
                else
                    buff = new p_SContainer(packet).Container;
                if (IsLoginCompleted)
                    crypt.Encrypt(ref buff);

                socketSAEA.SetBuffer(buff, 0, buff.Length);
                socket.SendAsync(socketSAEA);
            };
            sendQueue.Start(d);
            /*if (!isSend || t)
            {
                isSend = true;

                byte[] buff;
                if (!packet.IsToContainer)
                    buff = packet.Packet;
                else
                    buff = new p_SContainer(packet).Container;
                if (IsLoginCompleted)
                    crypt.Encrypt(ref buff);

                socketSAEA.SetBuffer(buff, 0, buff.Length);
                socket.SendAsync(socketSAEA);
            }
            else
                toSend.TryAdd(packet);*/
        }

        private void socket_Send(object obj, SocketAsyncEventArgs e)
        {
            sendQueue.End();
            /*if (toSend.Count != 0)
                sendAsync(toSend.Take(), true);
            else
                isSend = false;*/
        }

        private void socket_Receive(object obj, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                IsDisconnected = true;
                if (Disconnected != null)
                    Disconnected();
                return;
            }

            byte[] buff = GetArray(e.Buffer, 0, e.BytesTransferred);

            if (IsLoginCompleted)
                crypt.Decrypt(ref buff);

            //Начинаем обработку входящих данных
            lock (Wrapper)
            {
                var cont1 = PWStream.FromBuff(buff);
                foreach (var elm1 in cont1)
                    if (elm1.Header == 0x00)
                    {
                        var cont2 = PWStream.FromContainer(elm1);
                        foreach (var elm2 in cont2)
                            p(elm2);
                    }
                    else
                        p(elm1);
            }
            socket.ReceiveAsync(socketRAEA);
        }


        private void connect(string servAddr)
        {
            int port = 29000; IPAddress addr; IPEndPoint endPoint;
            string[] splited = servAddr.Split(new char[1] { ':' }, 2);

            try
            {
                addr = IPAddress.Parse(splited[0]);
            }
            catch
            {
                throw new ArgumentException("Ip-адрес сервера задан некорректно");
            }

            try
            {
                port = int.Parse(splited[1]);
            }
            catch
            {
                throw new ArgumentException("Порт сервера задан некорректно");
            }

            try
            {
                endPoint = new IPEndPoint(addr, port);
            }
            catch
            {
                throw new ArgumentException("Порт сервера задан некорректно, значение находится за границами допустимого");
            }
            try
            {
                socket.Connect(endPoint);
            }
            catch (SocketException e)
            {
                throw new Exception(string.Format("Подключение к серверу не удалось, {0}", e.Message));
            }
            socket.ReceiveAsync(socketRAEA);
        }
    }
}