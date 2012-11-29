using System;
using System.Collections.Generic;
using System.Text;

namespace CR1000Connection
{
    public class Server
    {
        public delegate void SentFileHandler(object sender, CoraScriptResultArgs sf);
        public event SentFileHandler SentFile;

        public delegate void ClockSetHandler(object sender, CoraScriptResultArgs sf);
        public event ClockSetHandler ClockSet;

        string host, port, username, password;
        string programPath;
        string dataLoggerName;
        List<String> responses;

        CsiCoraScriptLib.CsiCoraScriptControl coraScript;

        public Server(string host) : this(host, "6789", "", "") { }
        public Server() : this("localhost", "6789", "", "") { }
        public Server(string host, string port, string username, string password)
        {
            this.host       = host;
            this.port       = port;
            this.username   = username;
            this.password   = password;
            this.responses  = new List<String>();
            this.coraScript = new CsiCoraScriptLib.CsiCoraScriptControl();
            initializeDataLogger();
        }

        public String operationResult()
        {
            int n = responses.Count;
            if (n > 0)
            {
                return responses[n - 1];
            }
            return "No operations have been logged";
        }

        public void syncClocks(string dataLoggerName)
        {
            this.dataLoggerName = dataLoggerName;
            if (!coraScript.serverConnected)
            {
                coraScript.onServerConnectStarted +=
                    new CsiCoraScriptLib._ICsiCoraScriptControlEvents_onServerConnectStartedEventHandler(realSync);
                connect();
            }
            else
            {
                realSync();
            }
        }

        /// <summary>
        /// Sends a program file to the data logger specified. It will trigger two events
        /// depending on the send response (success / failure).
        /// </summary>
        /// <param name="dataLoggerName">The data logger name. A String.</param>
        /// <param name="programPath">The path of the file to send. A String.</param>
        public void sendProgramFile(string dataLoggerName, string programPath)
        {
            this.programPath    = programPath;
            this.dataLoggerName = dataLoggerName;

            if (!coraScript.serverConnected)
            {
                coraScript.onServerConnectStarted +=
                    new CsiCoraScriptLib._ICsiCoraScriptControlEvents_onServerConnectStartedEventHandler(realSendFile);
                connect();
            }
            else
            {
                realSendFile();
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // API Actions                                                                                  //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////

        private void realSync()
        {
            string command = "clock-set " + this.dataLoggerName;
            string response;

            coraScript.onServerConnectStarted -= new CsiCoraScriptLib._ICsiCoraScriptControlEvents_onServerConnectStartedEventHandler(realSync);
            response = coraScript.executeScript(command, 0);
            ClockSet(this, new CoraScriptResultArgs(response));
        }

        private void realSendFile()
        {
            string command = "send-file " + this.dataLoggerName + " " + this.programPath + " --run-now=true run-on-power-up=true";
            string response;

            coraScript.onServerConnectStarted -= new CsiCoraScriptLib._ICsiCoraScriptControlEvents_onServerConnectStartedEventHandler(realSendFile);
            response = coraScript.executeScript(command, 0);
            SentFile(this, new CoraScriptResultArgs(response));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // Server Event Responses                                                                       //////
        // Each of these methods will be called when an event happens after using the LoggerNet library //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This will run if the connection to LoggerNet was successful. It will log a message with information.
        /// </summary>
        private void loggerNetServerConnected()
        {
            try //implement error handling for this routine : try-catch
            {
                //Indicate success for server connect
                logResponse("+ Successfully connected to LoggerNet server " + host);
            }
            catch (Exception excp)
            {
                logResponse("- LoggerNet Connection Event : ERROR" + excp.Source + ": " + excp.Message);
            }
        }
        
        private void initializeDataLogger()
        {
            coraScript.onServerConnectStarted += new CsiCoraScriptLib._ICsiCoraScriptControlEvents_onServerConnectStartedEventHandler(loggerNetServerConnected);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // Server actions                                                                               //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        private void logResponse(string response)
        {
            responses.Add(response);
        }

        private void disconnect()
        {
            if (coraScript.serverConnected)
            {
                coraScript.serverDisconnect();
            }
        }


        /// <summary>
        /// Connect: Connects to the LoggerNet server using the params received on the
        /// constructor.
        /// 
        /// Depending on the connection result (success / failure) an event will be triggered
        /// and the methods `dataLoggerServerConnected` (success) and `dataLoggerServerConnectedFailure`
        /// will be executed.
        /// </summary>
        private void connect()
        {
            try
            {
                coraScript.Enabled = true;
                coraScript.serverName = this.host;
                coraScript.serverPort = Convert.ToInt16(this.port);
                coraScript.serverLogonName = this.username;
                coraScript.serverLogonPassword = this.password;

                if (!coraScript.serverConnected)
                {
                    coraScript.serverConnect();
                    logResponse("Corascript control has submitted connect command...");
                }
            }
            catch (Exception excp)
            {
                logResponse("- Connection Error. Could not connect. Info -> " + excp.Source + ": " + excp.Message);
            }
        }
    }
}
