using System;
using System.Collections.Generic;
using System.Text;

namespace CR1000Connection
{
    public class ClockSyncArgs : EventArgs
    {
        public ClockSyncArgs(bool successful, CSIDATALOGGERLib.clock_outcome_type response_code, DateTime current_date)
        {
            this.successful     = successful;
            this.current_date   = current_date;
            this.response_code  = (int)response_code;
        }
        public bool successful;
        public int response_code;
        public DateTime current_date;
    }

    public class SentFileArgs : EventArgs
    {
        public SentFileArgs(bool successful, CSIDATALOGGERLib.prog_send_outcome_type response_code, string compile_result)
        {
            this.successful     = successful;
            this.response_code  = (int) response_code;
            this.compile_result = compile_result;
        }
        public bool successful;
        public int response_code;
        public string compile_result;
    }

    public class Server
    {
        public delegate void ClockSyncHandler(object sender, ClockSyncArgs cs);
        public event ClockSyncHandler ClockSync;

        public delegate void SentFileHandler(object sender, SentFileArgs sf);
        public event SentFileHandler SentFile;

        string host, port, username, password;
        string programPath, programName;
        List<String> responses;
        private CSIDATALOGGERLib.DataLogger dataLogger;

        public Server() : this("localhost", "6789", "", "") { }
        public Server(string host, string port, string username, string password)
        {
            this.host       = host;
            this.port       = port;
            this.username   = username;
            this.password   = password;
            this.responses  = new List<String>();
            this.dataLogger = new CSIDATALOGGERLib.DataLogger();
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
            if (!dataLogger.serverConnected)
            {
                // First connect to the server, this function **will** connect to the datalogger
                // specified. So, we need to hook into the event of the DATALOGGER connection.
                dataLogger.onLoggerConnectStarted += new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectStartedEventHandler(realSync);
                connect(dataLoggerName);
            }
            else
            {
                dataLogger.clockSetStart();
            }
        }

        /// <summary>
        /// Sends a program file to the data logger specified. It will trigger two events
        /// depending on the send response (success / failure).
        /// </summary>
        /// <param name="dataLoggerName">The data logger name. A String.</param>
        /// <param name="programPath">The path of the file to send. A String.</param>
        /// <param name="retried">If we are retrying to send the same file. You should never use this attribute. A Boolean. Defaults to false.</param>
        public void sendProgramFile(string dataLoggerName, string programPath, string programName)
        {
            this.programPath = programPath;
            this.programName = programName;

            if (!dataLogger.serverConnected)
            {
                // First connect to the server, this function **will** connect to the datalogger
                // specified. So, we need to hook into the event of the DATALOGGER connection.
                dataLogger.onLoggerConnectStarted += new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectStartedEventHandler(realSendFile);
                connect(dataLoggerName);
            }
            else
            {
                dataLogger.programSendStart(programPath, programName);
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////// PRIVATE FUNCTIONS
        private void logResponse(string response)
        {
            responses.Add(response);
            //System.Console.WriteLine(response);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // Server actions                                                                               //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        private void disconnect()
        {
            if (dataLogger.serverConnected)
            {
                dataLogger.serverDisconnect();
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
        private void connect(string dataLoggerName)
        {
            try
            {
                //Set the connection parameters
                dataLogger.serverName = this.host;
                dataLogger.serverPort = Convert.ToInt16(this.port);
                dataLogger.serverLogonName = this.username;
                dataLogger.serverLogonPassword = this.password;
                dataLogger.loggerName = dataLoggerName;

                //Connect to the Loggernet server. If the connection
                //succeeds then the event OnServerConnectStarted() will be
                //called. Otherwise, the event onServerConnectFailure()
                //will be called.
                dataLogger.serverConnect();
            }
            catch (Exception excp)
            {
                logResponse("- Connection Error. Could not connect. Info -> " + excp.Source + ": " + excp.Message);
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // API Actions                                                                                  //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        private void realSync()
        {
            dataLogger.onLoggerConnectStarted -= new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectStartedEventHandler(realSync);
            dataLogger.clockSetStart(); // trigger the syncListener -> disconnect
        }

        private void realSendFile()
        {
            dataLogger.onLoggerConnectStarted -= new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectStartedEventHandler(realSendFile);
            dataLogger.programSendStart(programPath, programName); // trigger the programComplete -> disconnect
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // Server Event Responses                                                                       //////
        // Each of these methods will be called when an event happens after using the LoggerNet library //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This will run if the connection to LoggerNet was successful. It will log a message with information.
        /// </summary>
        private void dataLoggerServerConnected()
        {
            try //implement error handling for this routine : try-catch
            {
                //Indicate success for server connect
                logResponse("+ Successfully connected to LoggerNet server " + host);

                // Now that we are connected to LoggerNet, connect to the DataLogger
                dataLogger.loggerConnectStart(CSIDATALOGGERLib.logger_priority_type.lp_priority_high);
            }
            catch (Exception excp)
            {
                logResponse("- CSI Datalogger OnServerConnectStarted Event : ERROR" + excp.Source + ": " + excp.Message);
            }
        }

        /// <summary>
        /// This will run when connecting to LoggerNet fails
        /// </summary>
        /// <param name="failure_code"></param>
        private void dataLoggerServerConnectionFailed(CSIDATALOGGERLib.server_failure_type failure_code)
        {
            logResponse("- The connection to the LoggerNet server failed. Failure code: " + failure_code);
        }

        /// <summary>
        /// This will run when whaaat?
        /// </summary>
        private void dataLoggerClockCompleted(bool successful, CSIDATALOGGERLib.clock_outcome_type response_code, DateTime current_date)
        {
            try
            {
                if (successful)
                {
                    logResponse("+ Successfully synced clocks to " + current_date + ".");
                    // throw event Clock Sync Ok
                }
                else
                {
                    logResponse("- Could not get/set clock from data logger. Error code: " + response_code + ".");
                    // throw event Clock Sync Failed
                }
                ClockSyncArgs csArgs = new ClockSyncArgs(successful, response_code, current_date);
                ClockSync(this, csArgs);
            }
            catch (Exception excp)
            {
                logResponse("- CSI Datalogger onClockComplete Event : ERROR" + excp.Source + ": " + excp.Message);
                // throw event Clock Sync Failed
            }
        }

        /// <summary>
        /// This will run when we connect to a data logger.
        /// </summary>
        private void dataLoggerConnected()
        {
            logResponse("+ Successfully connected to data logger " + dataLogger.loggerName + "");
        }

        /// <summary>
        /// This will run when we could not connect to a data logger.
        /// </summary>
        private void dataLoggerConnectionFailure(CSIDATALOGGERLib.logger_failure_type fail_code)
        {
            logResponse("- The connection to the logger " + dataLogger.loggerName +" was not successful. Failure code: " + fail_code);
        }

        /// <summary>
        /// This will run when a program that was being sent to the data logger is complete.
        /// This means that the program will be sent & compiled.
        /// 
        /// If the send action failed, we will have the response code and compile result.
        /// </summary>
        /// <param name="successful"></param>
        /// <param name="response_code"></param>
        /// <param name="compile_result"></param>
        private void dataLoggerProgramSentComplete(bool successful, CSIDATALOGGERLib.prog_send_outcome_type response_code, string compile_result)
        {
            if (successful)
            {
                logResponse("+ Successfully sent the program. Complete event.");
            }
            else
            {
                logResponse("- Could not send the program. Response code: " + response_code+ ". Compile result: " + compile_result + ".");
            }
            SentFileArgs fsArgs = new SentFileArgs(successful, response_code, compile_result);
            SentFile(this, fsArgs);
        }
        
        private void initializeDataLogger()
        {
            dataLogger.onProgramSendComplete += new CSIDATALOGGERLib._IDataLoggerEvents_onProgramSendCompleteEventHandler(dataLoggerProgramSentComplete);
            dataLogger.onClockComplete += new CSIDATALOGGERLib._IDataLoggerEvents_onClockCompleteEventHandler(dataLoggerClockCompleted);

            dataLogger.onServerConnectStarted += new CSIDATALOGGERLib._IDataLoggerEvents_onServerConnectStartedEventHandler(dataLoggerServerConnected);
            dataLogger.onServerConnectFailure += new CSIDATALOGGERLib._IDataLoggerEvents_onServerConnectFailureEventHandler(dataLoggerServerConnectionFailed);

            dataLogger.onLoggerConnectStarted += new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectStartedEventHandler(dataLoggerConnected);
            dataLogger.onLoggerConnectFailure += new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectFailureEventHandler(dataLoggerConnectionFailure);
        }
    }
}
