using System;
namespace CR1000Connection
{
    public class ClockSyncArgs : EventArgs
    {
        public ClockSyncArgs(bool successful, CSIDATALOGGERLib.clock_outcome_type response_code, DateTime current_date)
        {
            this.successful = successful;
            this.current_date = current_date;
            this.response_code = (int)response_code;
        }
        public bool successful;
        public int response_code;
        public DateTime current_date;
    }

    public class SentFileArgs : EventArgs
    {
        public SentFileArgs(bool successful, CSIDATALOGGERLib.prog_send_outcome_type response_code, string compile_result)
        {
            this.successful = successful;
            this.response_code = (int)response_code;
            this.compile_result = compile_result;
        }
        public bool successful;
        public int response_code;
        public string compile_result;
    }

}