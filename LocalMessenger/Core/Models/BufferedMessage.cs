using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessenger.Core.Models
{
    public class BufferedMessage
    {
        public string ContactIP { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
