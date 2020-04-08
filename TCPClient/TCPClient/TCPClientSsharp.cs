using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using System.Collections.Generic;

namespace TCP_Client
{
    public class TCPClientSsharp
    {
        #region Properties

        private TCPClient _tcpClient;
        private bool _initialized { get; set; }
        private bool _debug { get; set; }
        private bool _manualDisconnect { get; set; }
        private CTimer _reconnectTimer;

        #endregion

        #region Delegates
        public delegate void ReceiveDataHandler(SimplSharpString data);
        public ReceiveDataHandler ReceiveData { get; set; }

        public delegate void ConnectedFbStatusHandler(ushort status);
        public ConnectedFbStatusHandler ConnectedFbStatus { get; set; }

        public delegate void ConnectionStatusHandler(SimplSharpString serialStatus, ushort analogStatus);
        public ConnectionStatusHandler ConnectionStatus { get; set; }

        public delegate void InitializedStatusHandler(ushort status);
        public InitializedStatusHandler InitializedStatus { get; set; }

        public delegate void ErrorStatusHandler(SimplSharpString error);
        public ErrorStatusHandler ErrorStatus { get; set; }
       
        #endregion

        #region S+ Methods

        /// <summary>
        /// initialize tcp client and subscribe to receive socket status change events
        /// return 1 to s+ if successful, else 0
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        public void Initialize(string ip, uint port, uint bufferSize)
        {
            _tcpClient = new TCPClient(ip, (int)port, (int)bufferSize);
            _tcpClient.SocketStatusChange += new TCPClientSocketStatusChangeEventHandler(OnSocketStatusChange);

            if (_tcpClient.PortNumber > 0 && _tcpClient.AddressClientConnectedTo != string.Empty)
            {
                _initialized = true;
                _manualDisconnect = false;
                InitializedStatus(Convert.ToUInt16(_initialized));
                CrestronConsole.PrintLine("TCPClient initialized: IP: {0}, Port: {1}",
                            _tcpClient.AddressClientConnectedTo, _tcpClient.PortNumber);
            }
            else
            {
                _initialized = false;
                Debug("TCPClient not initialized, missing data", ErrorLevel.Notice, "Missing data");
            }
        }

        /// <summary>
        /// connect to server and listen for data
        /// </summary>
        public void Connect()
        {
            SocketErrorCodes err = new SocketErrorCodes();
            if (_initialized)
            {
                try
                {
                    _manualDisconnect = false;
                    err = _tcpClient.ConnectToServerAsync(ConnectToServerCallback);
                    Debug("Connection attempt: " +_tcpClient.AddressClientConnectedTo, ErrorLevel.Notice, err.ToString());
                }
                catch (Exception e)
                {
                    Debug(e.Message, ErrorLevel.Error, err.ToString());
                }
            }
            else
            {
                Debug("TCPClient not initialized, missing data", ErrorLevel.Notice, err.ToString());
            }
        }

        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect()
        {
            SocketErrorCodes err = new SocketErrorCodes();
            try
            {
                _manualDisconnect = true;
                _tcpClient.Dispose();
                err = _tcpClient.DisconnectFromServer();
                Debug("Disconnect attempt: " + _tcpClient.AddressClientConnectedTo, ErrorLevel.Notice, err.ToString());
            }
            catch (Exception e)
            {
                Debug(e.Message, ErrorLevel.Error, err.ToString());
            }
        }

        /// <summary>
        /// enable logging to ErrorLog
        /// </summary>
        public void EnableDebug()
        {
            _debug = true;
            CrestronConsole.PrintLine("Debug Enabled");
        }

        /// <summary>
        /// disable logging to ErrorLog
        /// </summary>
        public void DisableDebug()
        {
            _debug = false;
            CrestronConsole.PrintLine("Debug Disabled");
        }

        /// <summary>
        /// send data to server
        /// </summary>
        /// <param name="tx">data</param>
        public void DataTransmit(SimplSharpString tx)
        {
            var err = new SocketErrorCodes();
            byte[] bytes = Encoding.UTF8.GetBytes(tx.ToString());
            err = _tcpClient.SendData(bytes, bytes.Length);
            Debug("Data transmitted: " + tx.ToString(), ErrorLevel.None, err.ToString());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// if client not connected, connect
        /// </summary>
        /// <param name="tcpClient"></param>
        private void ConnectToServerCallback(TCPClient tcpClient)
        {
            if (_tcpClient.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
                TryReconnect();
        }

        /// <summary>
        /// retry connection every 10s
        /// </summary>
        private void TryReconnect()
        {
            if (!_manualDisconnect)
            {
                Debug("Attempting to connect...", ErrorLevel.None, string.Empty);
                _reconnectTimer = new CTimer(o => { _tcpClient.ConnectToServerAsync(ConnectToServerCallback); }, 10000);
            }
        }

        /// <summary>
        /// event that triggers when socket status changes, send status to s+
        /// </summary>
        /// <param name="tcpClient">tcp client</param>
        /// <param name="sockStatus">socket status code</param>
        private void OnSocketStatusChange(TCPClient tcpClient, SocketStatus sockStatus)
        {
            if(_sockStatusDict.ContainsKey(sockStatus))
                ConnectionStatus(sockStatus.ToString(), _sockStatusDict[sockStatus]);

            if (sockStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                _tcpClient.ReceiveDataAsync(OnDataReceiveEventCallback);
                ConnectedFbStatus(1);
                _manualDisconnect = false;
            }
            else
            {
                ConnectedFbStatus(0);
                TryReconnect();
            }
        }

        /// <summary>
        /// create buffer for incoming data and pass to S+
        /// </summary>
        /// <param name="client">tcp client</param>
        /// <param name="bytes">buffer size</param>
        private void OnDataReceiveEventCallback(TCPClient client, int bytes)
        {
            byte[] rxBuffer;
            var rxToSplus = new SimplSharpString();
            if (bytes > 0)
            {
                rxBuffer = _tcpClient.IncomingDataBuffer;
                rxToSplus = Encoding.Default.GetString(rxBuffer, 0, bytes);
                ReceiveData(rxToSplus);
            }
            _tcpClient.ReceiveDataAsync(OnDataReceiveEventCallback);
        }

        private enum ErrorLevel { Notice, Warning, Error, None }

        /// <summary>
        /// Set Debug behavior
        /// </summary>
        /// <param name="msg">message to print to console</param>
        /// <param name="errLevel">set error severity</param>
        /// <param name="errCode">socket code status to send to s+</param>
        private void Debug(string msg, ErrorLevel errLevel, string errCode)
        {
            if (_debug)
            {
                CrestronConsole.PrintLine(msg);
                ErrorStatus(errCode);

                if (errLevel != ErrorLevel.None)
                {
                    switch (errLevel)
                    {
                        case ErrorLevel.Notice:
                            ErrorLog.Notice(msg);
                            break;
                        case ErrorLevel.Warning:
                            ErrorLog.Warn(msg);
                            break;
                        case ErrorLevel.Error:
                            ErrorLog.Error(msg);
                            break;
                    }
                }
            }
        }
        #endregion

        #region SocketStatus Dictionary
        private Dictionary<SocketStatus, ushort> _sockStatusDict = new Dictionary<SocketStatus, ushort>()
        {
            {SocketStatus.SOCKET_STATUS_NO_CONNECT, 0},
            {SocketStatus.SOCKET_STATUS_WAITING, 1},
            {SocketStatus.SOCKET_STATUS_CONNECTED, 2},
            {SocketStatus.SOCKET_STATUS_CONNECT_FAILED, 3},
            {SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY, 4},
            {SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY, 5},
            {SocketStatus.SOCKET_STATUS_DNS_LOOKUP, 6},
            {SocketStatus.SOCKET_STATUS_DNS_FAILED, 7},
            {SocketStatus.SOCKET_STATUS_DNS_RESOLVED, 8},
            {SocketStatus.SOCKET_STATUS_LINK_LOST,9},
            {SocketStatus.SOCKET_STATUS_SOCKET_NOT_EXIST,10}
        };
        #endregion
    }
}
