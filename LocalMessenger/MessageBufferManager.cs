using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LocalMessenger
{
    public class MessageBufferManager
    {
        private readonly string bufferPath;
        private List<BufferedMessage> buffer;

        public MessageBufferManager(string appDataPath)
        {
            bufferPath = Path.Combine(appDataPath, "message_buffer.json");
            buffer = LoadBuffer();
        }

        public void AddToBuffer(string contactIP, string message)
        {
            buffer.Add(new BufferedMessage
            {
                ContactIP = contactIP,
                Message = message,
                Timestamp = DateTime.Now
            });
            SaveBuffer();
        }

        public List<BufferedMessage> GetBuffer()
        {
            return buffer;
        }

        public void RemoveFromBuffer(BufferedMessage message)
        {
            buffer.Remove(message);
            SaveBuffer();
        }

        public void SaveBuffer()
        {
            var json = JsonConvert.SerializeObject(buffer, Formatting.Indented);
            File.WriteAllText(bufferPath, json);
        }

        private List<BufferedMessage> LoadBuffer()
        {
            if (File.Exists(bufferPath))
            {
                try
                {
                    var json = File.ReadAllText(bufferPath);
                    return JsonConvert.DeserializeObject<List<BufferedMessage>>(json) ?? new List<BufferedMessage>();
                }
                catch
                {
                    return new List<BufferedMessage>();
                }
            }
            return new List<BufferedMessage>();
        }
    }

    public class BufferedMessage
    {
        public string ContactIP { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}