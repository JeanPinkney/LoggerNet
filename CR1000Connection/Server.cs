using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CR1000Connection
{
    public class Server
    {
        string host, port, username, password;
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

        public Boolean syncClocks(string dataLoggerName)
        {
            // connect to server
            // sync clocks to this datalogger
            // check result and log it
            return true;
        }

        public Boolean shutDownLogger(string dataLoggerName)
        {
            // connect to server
            // send an empty program. Use sendProgramFile
            // check result and log it
            return true;
        }

        /// <summary>
        /// Sends a program file to the data logger specified. It will trigger two events
        /// depending on the send response (success / failure).
        /// </summary>
        /// <param name="dataLoggerName">The data logger name. A String.</param>
        /// <param name="programPath">The path of the file to send. A String.</param>
        /// <param name="retried">If we are retrying to send the same file. You should never use this attribute. A Boolean. Defaults to false.</param>
        public void sendProgramFile(string dataLoggerName, string programPath, bool retried = false)
        {
            try //implement error handling for this routine : try-catch
            {
                //Send a program to the datalogger
                if (dataLogger.serverConnected)
                {
                    if (programPath == "")
                    {
                        logResponse("- Please specify a program to send");
                    }
                    else
                    {
                        dataLogger.programSendStart(programPath, "");
                    }
                }
                else if (!retried) // Only retry once. If not, just die.
                {
                    connect();
                    sendProgramFile(dataLoggerName, programPath, true);
                }
                else
                {
                    logResponse("- There was an error connecting to the server and could not send the file.");
                }

            }
            catch (Exception excp)
            {
                logResponse("- CSI Datalogger Send Program Button : ERROR" + excp.Source + ": " + excp.Message);
            }
        }

        ////////////////////////////////////////////////// PRIVATE FUNCTIONS

        private void logResponse(string response)
        {
            responses.Add(response);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // Server actions                                                                               //////
        //////////////////////////////////////////////////////////////////////////////////////////////////////

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
                //Set the connection parameters
                dataLogger.serverName = this.host;
                dataLogger.serverPort = Convert.ToInt16(this.port);
                dataLogger.serverLogonName = this.username;
                dataLogger.serverLogonPassword = this.password;

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
                }
                else
                {
                    logResponse("- Could not get/set clock from data logger. Error code: " + response_code  + ".");
                }
            }
            catch (Exception excp)
            {
                logResponse("- CSI Datalogger onClockComplete Event : ERROR" + excp.Source + ": " + excp.Message);
            }
        }

        /// <summary>
        /// This will run when we connect to a data logger.
        /// </summary>
        private void dataLoggerConnected()
        {
            logResponse("+ Successfully connected to data logger.");
        }

        /// <summary>
        /// This will run when a program that was being sent to the data logger is complete.
        /// This means that the program will be sent & compiled.
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
        }
        
        private void initializeDataLogger()
        {
            dataLogger.onProgramSendComplete += new CSIDATALOGGERLib._IDataLoggerEvents_onProgramSendCompleteEventHandler(dataLoggerProgramSentComplete);
            dataLogger.onClockComplete += new CSIDATALOGGERLib._IDataLoggerEvents_onClockCompleteEventHandler(dataLoggerClockCompleted);

            dataLogger.onServerConnectStarted += new CSIDATALOGGERLib._IDataLoggerEvents_onServerConnectStartedEventHandler(dataLoggerServerConnected);
            dataLogger.onServerConnectFailure += new CSIDATALOGGERLib._IDataLoggerEvents_onServerConnectFailureEventHandler(dataLoggerServerConnectionFailed);

            dataLogger.onLoggerConnectStarted += new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectStartedEventHandler(dataLoggerConnected);
            //this.dataLogger.onLoggerConnectFailure += new CSIDATALOGGERLib._IDataLoggerEvents_onLoggerConnectFailureEventHandler(this.axDataLogger1_onLoggerConnectFailure);
        }
    }
}
