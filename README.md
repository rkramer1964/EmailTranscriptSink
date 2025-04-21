# EmailTranscriptSink

Serilog sink that captures a log for the entire run to an internal stream, then emails it via MailKit.  Solves the BufferedSink problem of holding messages back when BatchSizeLimit or BufferingTimeLimit are set