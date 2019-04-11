using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Models
{
    public class ExtractedData
    {
        public IList<Uri> Links { get; set; } = new List<Uri>();
        public IList<string> Data { get; set; } = new List<string>();
    }
}
