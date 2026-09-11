namespace GromCore.Laser.Server.Message
{
    using GromCore.Laser.Server.Networking;
    using GromCore.Laser.Server.Message;
    using System.Collections.Concurrent;
    using GromCore.Laser.Logic.Message;

    public static class Processor
    {
        private static ConcurrentQueue<QueueItem> IncomingQueue;
        private static ConcurrentQueue<QueueItem> OutgoingQueue;

        private static ManualResetEvent ReceiveEvent;
        private static ManualResetEvent SendEvent;

        private static Thread ReceiveThread;
        private static Thread SendThread;

        private struct QueueItem
        {
            public readonly Connection Connection;
            public readonly GameMessage Message;

            public QueueItem(Connection connection, GameMessage message)
            {
                Connection = connection;
                Message = message;
            }
        }

        public static void Init()
        {
            IncomingQueue = new ConcurrentQueue<QueueItem>();
            OutgoingQueue = new ConcurrentQueue<QueueItem>();

            ReceiveEvent = new ManualResetEvent(false);
            SendEvent = new ManualResetEvent(false);

            ReceiveThread = new Thread(UpdateReceive);
            SendThread = new Thread(UpdateSend);

            ReceiveThread.Start();
            SendThread.Start();
        }

        public static bool Receive(Connection connection, GameMessage message)
        {
            if (message == null) return false;

            if (IncomingQueue.Count >= 1024)
            {
                Logger.Print($"Processor: Incoming message queue full. Message of type {message.GetMessageType()} discarded.");
                return false;
            }

            IncomingQueue.Enqueue(new QueueItem(connection, message));
            ReceiveEvent.Set();
            return true;
        }

        public static void Send(Connection connection, GameMessage message)
        {
            if (message == null) return;

            try
            {
                // Freeze the payload on the caller thread. Queueing a mutable message and
                // encoding it later allowed reward lists to change during enumeration.
                if (message.GetEncodingLength() == 0)
                {
                    message.Encode();
                }
            }
            catch (Exception exception)
            {
                Logger.Print($"Processor: failed to encode message {message.GetMessageType()}: {exception}");
                return;
            }

            if (OutgoingQueue.Count >= 1024)
            {
                Logger.Print($"Processor: Outgoing message queue full. Message of type {message.GetMessageType()} discarded.");
                return;
            }

            OutgoingQueue.Enqueue(new QueueItem(connection, message));
            SendEvent.Set();
        }

        public static void UpdateReceive()
        {
            while (true)
            {
                ReceiveEvent.WaitOne();

                while (IncomingQueue.TryDequeue(out QueueItem item))
                {
                    try
                    {
                        item.Connection.MessageManager.ReceiveMessage(item.Message);
                    }
                    catch (Exception) { }
                }

                ReceiveEvent.Reset();
            }
        }

        public static void UpdateSend()
        {
            while (true)
            {
                SendEvent.WaitOne();

                while (OutgoingQueue.TryDequeue(out QueueItem item))
                {
                    try
                    {
                        item.Connection.Messaging.EncryptAndWrite(item.Message);
                    }
                    catch (Exception exception)
                    {
                        Logger.Print($"Processor: failed to send message {item.Message.GetMessageType()}: {exception}");
                    }
                }

                SendEvent.Reset();
            }
        }
    }
}
