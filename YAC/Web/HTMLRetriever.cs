using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YAC.Web
{
    public static class HTMLRetriever
    {
        public static string GetHTML(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                // read out contents
                return reader.ReadToEnd();
            }
        }
    }
}
