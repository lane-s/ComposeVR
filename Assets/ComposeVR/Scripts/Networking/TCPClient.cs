using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;
using System.IO;
using Google.Protobuf;

namespace ComposeVR {

    [RequireComponent(typeof(RemoteEventRouter))]
    public class TCPClient : MonoBehaviour {

        public string HostIP;
        public int HostPort;

        private TcpClient client;

        private float maxUpdateTime = 1.0f;

        private Queue<Protocol.Event> eventQueue;
        private System.Object eventQueueLock = new System.Object();

        private MemoryStream inStream;
        private MemoryStream lengthStream;
        private MemoryStream messageStream;

        private enum InputState { READING_LENGTH, READING_MESSAGE, COMPLETETING_PARTIAL_MESSAGE }
        private InputState inputState;

        private MemoryStream outStream;

        private int nextMessageLength;

        // Use this for initialization
        void Start() {
            eventQueue = new Queue<Protocol.Event>();

            inStream = new MemoryStream();
            lengthStream = new MemoryStream();
            messageStream = new MemoryStream();
            
            inputState = InputState.READING_LENGTH;

            outStream = new MemoryStream();

            try {
                client = new TcpClient(AddressFamily.InterNetwork);
                IPAddress[] remoteHost = Dns.GetHostAddresses(HostIP);
                client.BeginConnect(remoteHost, HostPort, new
                    AsyncCallback(ConnectCallback), client);
            }
            catch (Exception e) {
                Debug.Log("Can't connect");
            }

            DontDestroyOnLoad(this.gameObject);
        }

        private void ConnectCallback(IAsyncResult result) {
            try {
                NetworkStream networkStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                networkStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
            }
            catch (Exception ex) {
                Debug.Log("Connection callback failed");
            }
        }

        /// <summary>
        /// This method parses an event from a stream and queues it
        /// </summary>
        /// <param name="s"> This stream should contain one complete event with no delimiter </param>
        private void consumeMessage(MemoryStream s) {
            lock (eventQueueLock) {
                s.Seek(0, SeekOrigin.Begin);
                Protocol.Event incomingEvent = Protocol.Event.Parser.ParseFrom(s);
                eventQueue.Enqueue(incomingEvent);

                //Clear the stream
                s.SetLength(0);
                lengthStream.SetLength(0);
            }
        }

        /// <summary>
        /// Reads one byte of a varint into lengthStream
        /// </summary>
        /// <param name="s">The stream to read</param>
        /// <returns> -1 if not finished reading or the value of the varint if finished </returns>
        private int ReadVarintByte(MemoryStream s) {
            byte nextByte = (byte)s.ReadByte();
            lengthStream.WriteByte(nextByte);

            int endOfInt = 0x80 & nextByte; //The most significant bit of each byte is set when there is still more to read
            if (endOfInt == 0) {
                //We have the entire varint, so read it
                CodedInputStream coded = new CodedInputStream(lengthStream, true);
                lengthStream.Seek(0, SeekOrigin.Begin);

                int result = coded.ReadLength();

                coded.Dispose();
                lengthStream.SetLength(0);

                return result;
            }

            return -1;
        }

        private void ReadCallback(IAsyncResult result) {

            NetworkStream networkStream;

            try {
                networkStream = client.GetStream();
            }
            catch {
                Debug.Log("Read exception");
                return;
            }

            byte[] buffer = result.AsyncState as byte[];
            int bytesRead = networkStream.EndRead(result);
           
            if (bytesRead > 0) {
                inStream.Write(buffer, 0, bytesRead);
                inStream.Seek(0, SeekOrigin.Begin);

                while (inStream.Position < inStream.Length) {

                    long inputBytesRemaining = inStream.Length - inStream.Position;

                    switch (inputState) {
                        case InputState.READING_LENGTH: //Read a base 128 varint from the input stream
                            nextMessageLength = ReadVarintByte(inStream);
                            if(nextMessageLength != -1) {
                                inputState = InputState.READING_MESSAGE;
                            }
                            break;
                        case InputState.READING_MESSAGE:
                            if(nextMessageLength <= inputBytesRemaining) {
                                for(int i = 0; i < nextMessageLength; i++) {
                                    //Read the remaining message into the message stream
                                    messageStream.WriteByte((byte)inStream.ReadByte());
                                }

                                consumeMessage(messageStream);
                                inputState = InputState.READING_LENGTH;
                            }
                            else {
                                //We don't have the entire message yet, read the rest of the input into the message stream and wait for more input
                                while(inStream.Position < inStream.Length) {
                                    messageStream.WriteByte((byte)inStream.ReadByte());
                                }

                                inputState = InputState.COMPLETETING_PARTIAL_MESSAGE;
                            }

                            break;
                        case InputState.COMPLETETING_PARTIAL_MESSAGE:
                            long messageBytesRemaining = nextMessageLength - messageStream.Length;

                            if(messageBytesRemaining <= inputBytesRemaining) {
                                //We can complete the partial message
                                for(int i = 0; i < messageBytesRemaining; i++) {
                                    messageStream.WriteByte((byte)inStream.ReadByte());
                                }

                                consumeMessage(messageStream);
                                inputState = InputState.READING_LENGTH;
                            }
                            else {
                                //The current input can't finish the partial message, so store it in the message stream
                                while(inStream.Position < inStream.Length) {
                                    messageStream.WriteByte((byte)inStream.ReadByte());
                                }
                            }
                            break;
                        default:
                            Debug.LogError("Invalid input state");
                            break;
                    }
                }

                //Reset the input stream
                inStream.SetLength(0);
            }

            networkStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
        }

        private void WriteCallback(IAsyncResult result) {

            NetworkStream networkStream;

            try {
                networkStream = client.GetStream();
                networkStream.EndWrite(result);
            }
            catch {
                Debug.Log("Write exception");
                return;
            }

        }

        // Update is called once per frame
        void Update() {
            int eventCount = 0;

            lock (eventQueueLock) {
                if (eventQueue != null) {
                    eventCount = eventQueue.Count;
                }
            }

            //Set the time to stop processing incoming events, otherwise the frame may hang
            float timeout = Time.realtimeSinceStartup + maxUpdateTime;

            while (eventCount > 0) {

                if (Time.realtimeSinceStartup > timeout) {
                    break;
                }

                //Try to get an event from the queue
                Protocol.Event currentEvent = null;
                lock (eventQueueLock) {
                    currentEvent = eventQueue.Dequeue();
                }

                //Send the event on to its target object
                if (currentEvent != null) {
                    GetComponent<RemoteEventRouter>().routeEvent(currentEvent);
                }

                eventCount--;
            }
        }

        void OnApplicationQuit() {
            try {
                client.Close();
            }
            catch (Exception e) {
                Debug.Log("Can't close connection");
            }
        }

        public void send(Protocol.Event outgoingEvent) {
            Debug.Log("Sending event " + outgoingEvent.MethodName);
            NetworkStream networkStream = client.GetStream();

            //Clear the output stream
            outStream.SetLength(0);
            
            //Bitwig requires a 32bit integer header specifying the length of the data to follow 
            byte[] header = BitConverter.GetBytes(outgoingEvent.CalculateSize() + 1); //Add 1 to the calculated size to account for the delimiter

            //The header must be in big endian format
            if (BitConverter.IsLittleEndian)
                Array.Reverse(header, 0, header.Length);

            //Write the data and the header to the stream
            outStream.Write(header, 0, header.Length);
            outgoingEvent.WriteDelimitedTo(outStream);

            //Get the data from the memory stream and write it to the network stream
            byte[] data = outStream.ToArray();
            networkStream.BeginWrite(data, 0, data.Length, new AsyncCallback(WriteCallback), data);
        }

    }
}