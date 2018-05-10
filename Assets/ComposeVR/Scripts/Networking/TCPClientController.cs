using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using Google.Protobuf;
using System.Net;
using UnityEngine;

namespace ComposeVR
{
    [Serializable]
    public class TCPClientController : IEventEmitter
    {

        private IEventQueue EventQueue;

        [Serializable]
        public class TCPClientConfiguration
        {
            public string HostIP;
            public int HostPort;
        }

        public enum InputState { READING_LENGTH, READING_MESSAGE, COMPLETETING_PARTIAL_MESSAGE }

        [Serializable]
        public class TCPClientState
        {
            public TcpClient Client;

            public MemoryStream InputStream;
            public MemoryStream LengthStream;
            public MemoryStream PartialMessageStream;

            public InputState InputState;

            public MemoryStream OutStream;
        }

        public TCPClientConfiguration Config;
        private TCPClientState State = new TCPClientState();

        private int nextMessageLength;
        private long messageStartLoc;

        public void SetEventQueue(IEventQueue q)
        {
            EventQueue = q;
        }

        public void Initialize()
        {

            State.InputStream = new MemoryStream();
            State.LengthStream = new MemoryStream();
            State.PartialMessageStream = new MemoryStream();

            State.InputState = InputState.READING_LENGTH;

            State.OutStream = new MemoryStream();

            //Use this client to emit events over the network
            RemoteEventEmitter.Instance.SetEmitter(this);

            try
            {
                State.Client = new TcpClient(AddressFamily.InterNetwork);
                IPAddress[] remoteHost = Dns.GetHostAddresses(Config.HostIP);
                State.Client.BeginConnect(remoteHost, Config.HostPort, new
                    AsyncCallback(ConnectCallback), State.Client);
            }
            catch (Exception e)
            {
                Debug.Log("Can't connect");
            }
        }

        /// <summary>
        /// This method parses an event from a stream and queues it
        /// </summary>
        /// <param name="s"> This stream should contain one complete event with no delimiter </param>
        private void QueueNextMessageInStream(MemoryStream s)
        {
            s.Seek(messageStartLoc, SeekOrigin.Begin);

            Protocol.Event incomingEvent = Protocol.Event.Parser.ParseDelimitedFrom(s);
            EventQueue.QueueEvent(incomingEvent);

            State.LengthStream.SetLength(0);
        }

        /// <summary>
        /// Reads one byte of a varint into lengthStream
        /// </summary>
        /// <param name="s">The stream to read</param>
        /// <returns> -1 if not finished reading or the value of the varint if finished </returns>
        private int ReadVarintByte(MemoryStream s)
        {
            byte nextByte = (byte)s.ReadByte();
            State.LengthStream.WriteByte(nextByte);

            int endOfInt = 0x80 & nextByte; //The most significant bit of each byte is set when there is still more to read
            if (endOfInt == 0)
            {
                //We have the entire varint, so read it
                CodedInputStream coded = new CodedInputStream(State.LengthStream, true);
                State.LengthStream.Seek(0, SeekOrigin.Begin);

                int result = coded.ReadLength();

                coded.Dispose();
                return result;
            }

            return -1;
        }

        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                NetworkStream networkStream = State.Client.GetStream();
                byte[] buffer = new byte[State.Client.ReceiveBufferSize];

                networkStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
                Debug.Log("Connected");
            }
            catch (Exception ex)
            {
                Debug.Log("Connection callback failed");
            }
        }

        private void ReadCallback(IAsyncResult result)
        {

            NetworkStream networkStream;

            try
            {
                networkStream = State.Client.GetStream();
            }
            catch
            {
                //Debug.Log("Read exception");
                return;
            }

            byte[] buffer = result.AsyncState as byte[];
            int bytesRead = networkStream.EndRead(result);

            if (bytesRead > 0)
            {
                State.InputStream.Write(buffer, 0, bytesRead);
                State.InputStream.Seek(0, SeekOrigin.Begin);

                messageStartLoc = 0;

                while (State.InputStream.Position < State.InputStream.Length)
                {

                    long inputBytesRemaining = State.InputStream.Length - State.InputStream.Position;

                    switch (State.InputState)
                    {
                        case InputState.READING_LENGTH: //Read a base 128 varint from the input stream
                            nextMessageLength = ReadVarintByte(State.InputStream);
                            if (nextMessageLength != -1)
                            {
                                State.InputState = InputState.READING_MESSAGE;
                            }
                            break;
                        case InputState.READING_MESSAGE:
                            if (nextMessageLength <= inputBytesRemaining)
                            {
                                QueueNextMessageInStream(State.InputStream);

                                messageStartLoc = State.InputStream.Position;
                                State.InputState = InputState.READING_LENGTH;
                            }
                            else
                            {
                                //We don't have the entire message yet, so store a partial message
                                State.LengthStream.Seek(0, SeekOrigin.Begin);
                                State.LengthStream.WriteTo(State.PartialMessageStream);

                                while (State.InputStream.Position < State.InputStream.Length)
                                {
                                    State.PartialMessageStream.WriteByte((byte)State.InputStream.ReadByte());
                                }

                                State.InputState = InputState.COMPLETETING_PARTIAL_MESSAGE;
                            }

                            break;
                        case InputState.COMPLETETING_PARTIAL_MESSAGE:
                            long messageBytesRemaining = nextMessageLength - (State.PartialMessageStream.Length - State.LengthStream.Length);

                            if (messageBytesRemaining <= inputBytesRemaining)
                            {
                                //We can complete the partial message
                                for (int i = 0; i < messageBytesRemaining; i++)
                                {
                                    State.PartialMessageStream.WriteByte((byte)State.InputStream.ReadByte());
                                }

                                QueueNextMessageInStream(State.PartialMessageStream);
                                State.PartialMessageStream.SetLength(0);

                                messageStartLoc = State.InputStream.Position;
                                State.InputState = InputState.READING_LENGTH;
                            }
                            else
                            {
                                //The current input can't finish the partial message, so store it in the message stream
                                while (State.InputStream.Position < State.InputStream.Length)
                                {
                                    State.PartialMessageStream.WriteByte((byte)State.InputStream.ReadByte());
                                }
                            }
                            break;
                        default:
                            Debug.LogError("Invalid input state");
                            break;
                    }
                }

                //Reset the input stream
                State.InputStream.SetLength(0);
            }

            networkStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
        }

        private void WriteCallback(IAsyncResult result)
        {

            NetworkStream networkStream;

            try
            {
                networkStream = State.Client.GetStream();
                networkStream.EndWrite(result);
            }
            catch
            {
                Debug.Log("Write exception");
                return;
            }

        }

        public void Close()
        {
            try
            {
                State.Client.Close();
            }
            catch (Exception e)
            {
                Debug.Log("Can't close connection");
            }
        }

        public void EmitEvent(Protocol.Event outgoingEvent)
        {
            try
            {
                //Debug.Log("Sending event " + outgoingEvent.MethodName);
                NetworkStream networkStream = State.Client.GetStream();

                //Clear the output stream
                State.OutStream.SetLength(0);

                //Bitwig requires a 32bit integer header specifying the length of the data to follow 
                byte[] header = BitConverter.GetBytes(outgoingEvent.CalculateSize() + 1); //Add 1 to the calculated size to account for the delimiter

                //The header must be in big endian format
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(header, 0, header.Length);

                //Write the data and the header to the stream
                State.OutStream.Write(header, 0, header.Length);
                outgoingEvent.WriteDelimitedTo(State.OutStream);

                //Get the data from the memory stream and write it to the network stream
                byte[] data = State.OutStream.ToArray();
                networkStream.BeginWrite(data, 0, data.Length, new AsyncCallback(WriteCallback), data);
            }
            catch (IOException e)
            {
                Debug.LogError("Can't emit event. Not connected.");
            }
        }

    }

    public interface IEventEmitter
    {
        void EmitEvent(Protocol.Event e);
    }
}
