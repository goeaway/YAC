using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Exceptions
{
    public class YACException : Exception
    {
        public YACException() { }
        public YACException(string message) : base(message) { }
        public YACException(string message, Exception innerException) : base(message, innerException) { }
    }
}
