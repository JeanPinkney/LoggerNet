using System;
namespace CR1000Connection
{
    public class CoraScriptResultArgs : EventArgs
    {
        public CoraScriptResultArgs(string result)
        {
            this.result     = result;
            this.successful = result[0] == '+';
        }
        public bool successful;
        public string result;
    }
}