namespace TCP_Client;
        // class declarations
         class TCPClientSsharp;
     class TCPClientSsharp 
    {
        // class delegates
        delegate FUNCTION ReceiveDataHandler ( SIMPLSHARPSTRING data );
        delegate FUNCTION ConnectedFbStatusHandler ( INTEGER status );
        delegate FUNCTION ConnectionStatusHandler ( SIMPLSHARPSTRING serialStatus , INTEGER analogStatus );
        delegate FUNCTION InitializedStatusHandler ( INTEGER status );
        delegate FUNCTION ErrorStatusHandler ( SIMPLSHARPSTRING error );

        // class events

        // class functions
        FUNCTION Initialize ( STRING ip , LONG_INTEGER port , LONG_INTEGER bufferSize );
        FUNCTION Connect ();
        FUNCTION Disconnect ();
        FUNCTION EnableDebug ();
        FUNCTION DisableDebug ();
        FUNCTION DataTransmit ( SIMPLSHARPSTRING tx );
        STRING_FUNCTION ToString ();
        SIGNED_LONG_INTEGER_FUNCTION GetHashCode ();

        // class variables
        INTEGER __class_id__;

        // class properties
        DelegateProperty ReceiveDataHandler ReceiveData;
        DelegateProperty ConnectedFbStatusHandler ConnectedFbStatus;
        DelegateProperty ConnectionStatusHandler ConnectionStatus;
        DelegateProperty InitializedStatusHandler InitializedStatus;
        DelegateProperty ErrorStatusHandler ErrorStatus;
    };

