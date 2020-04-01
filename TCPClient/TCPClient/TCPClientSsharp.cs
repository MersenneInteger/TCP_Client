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
        public void Initialize(string ip, uint port, uint bufferSize)
        {
            _tcpClient = new TCPClient(ip, (int)port, (int)bufferSize);
            _tcpClient.SocketStatusChange += new TCPClientSocketStatusChangeEventHandler(OnSocketStatusChange);

            if (_tcpClient.PortNumber > 0 && _tcpClient.AddressClientConnectedTo != string.Empty)
            {
                _initialized = true;
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

        public void Connect()
        {
            SocketErrorCodes err = new SocketErrorCodes();
            if (_initialized)
            {
                try
                {
                    err = _tcpClient.ConnectToServer();
                    if (err == 0)
                        err = _tcpClient.ReceiveDataAsync(OnDataReceiveEventCallback);
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

        public void Disconnect()
        {
            SocketErrorCodes err = new SocketErrorCodes();
            try
            {
                _tcpClient.Dispose();
                err = _tcpClient.DisconnectFromServer();
                Debug("Disconnect attempt: " + _tcpClient.AddressClientConnectedTo, ErrorLevel.Notice, err.ToString());
            }
            catch (Exception e)
            {
                Debug(e.Message, ErrorLevel.Error, err.ToString());
            }
        }

        public void EnableDebug()
        {
            _debug = true;
            CrestronConsole.PrintLine("Debug Enabled");
        }

        public void DisableDebug()
        {
            _debug = false;
            CrestronConsole.PrintLine("Debug Disabled");
        }

        public void DataTransmit(SimplSharpString tx)
        {
            var err = new SocketErrorCodes();
            byte[] bytes = Encoding.UTF8.GetBytes(tx.ToString());
            err = _tcpClient.SendData(bytes, bytes.Length);
            Debug("Data transmitted: " + tx.ToString(), ErrorLevel.None, err.ToString());
        }

        #endregion

        #region Private Methods

        private void OnSocketStatusChange(TCPClient tcpClient, SocketStatus sockStatus)
        {
            if(_sockStatusDict.ContainsKey(sockStatus))
                ConnectionStatus(sockStatus.ToString(), _sockStatusDict[sockStatus]);
            if (sockStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                ConnectedFbStatus(1);
            else
            {
                ConnectedFbStatus(0);
                if (sockStatus == SocketStatus.SOCKET_STATUS_CONNECT_FAILED
                    || sockStatus == SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY
                    || sockStatus == SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY)
                {
                    Connect();
                }
            }
        }

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
