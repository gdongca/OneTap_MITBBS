using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace HtmlAgilityPack
{
    public abstract class MimePart
    {
        Dictionary<string, string> _headers = new Dictionary<string, string>();
        byte[] _header;

        public Dictionary<string, string> Headers
        {
            get { return _headers; }
        }

        public byte[] Header
        {
            get { return _header; }
        }

        public long GenerateHeaderFooterData(string boundary)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("--");
            sb.Append(boundary);
            sb.AppendLine();
            foreach (string key in _headers.Keys)
            {
                sb.Append(key);
                sb.Append(": ");
                sb.AppendLine(_headers[key]);
            }
            sb.AppendLine();

            _header = Encoding.UTF8.GetBytes(sb.ToString());

            return _header.Length + Data.Length + 2;
        }

        public abstract Stream Data { get; }
    }

    public class StringMimePart : MimePart
    {
        Stream _data;

        public string StringData
        {
            set
            {
                _data = new MemoryStream(Encoding.UTF8.GetBytes(value));
            }
        }

        public override Stream Data
        {
            get
            {
                return _data;
            }
        }
    }

    public class StreamMimePart : MimePart
    {
        Stream _data;

        public void SetStream(Stream stream)
        {
            _data = stream;
        }

        public override Stream Data
        {
            get
            {
                return _data;
            }
        }
    }
}