using System;
using System.Net;
using System.Text;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Windows.Threading;

namespace HtmlAgilityPack
{
    /// <summary>
    ///  A cookie aware web client class
    /// </summary>
    public class CookieAwareClient : WebClient
    {
        [System.Security.SecuritySafeCritical]
        public CookieAwareClient()
            : base()
        {
        }
        
        public CookieContainer Cookies;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            if (Cookies != null)
            {
                if (request is HttpWebRequest)
                {
                    (request as HttpWebRequest).CookieContainer = Cookies;
                }
            }

            return request;
        }

    }

    /// <summary>
    /// Represents a combined list and collection of Form Elements.
    /// </summary>
    public class FormElementCollection : Dictionary<string, string>
    {
        /// <summary>
        /// Constructor. Parses the HtmlDocument to get all form input elements. 
        /// </summary>
        public FormElementCollection(HtmlNode htmlDocNode)
        {
            var inputs = htmlDocNode.Descendants("input");
            foreach (var element in inputs)
            {
                string name = element.GetAttributeValue("name", "undefined");
                string value = element.GetAttributeValue("value", "");

                if (!name.Equals("undefined"))
                {
                    if (ContainsKey(name))
                    {
                        Remove(name);
                    }

                    Add(name, value);
                }
            }

            var textareas = htmlDocNode.Descendants("textarea");
            foreach (var element in textareas)
            {
                string name = element.GetAttributeValue("name", "undefined");
                string value;

                if (element.FirstChild != null)
                {
                    value = HttpUtility.HtmlDecode(element.FirstChild.InnerText);
                }
                else
                {
                    value = "";
                }

                if (!name.Equals("undefined"))
                {
                    if (ContainsKey(name))
                    {
                        Remove(name);
                    }

                    Add(name, value);
                }
            }
        }

        /// <summary>
        /// Assembles all form elements and values to POST. Also html encodes the values.  
        /// </summary>
        public string AssemblePostPayload()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var element in this)
            {
                string value;

                value = UrlEncode(element.Value);
                
                sb.Append("&" + element.Key + "=" + value);
            }
            return sb.ToString().Substring(1);
        }

        private string UrlEncode(string value)
        {
            StringBuilder sb = new StringBuilder();
            char [] chars = value.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                switch (c)
                {
                    case ';':
                    case '/':
                    case '?':
                    case ':':
                    case '@':
                    case '&':
                    case '=':
                    case '+':
                    case '$':
                    case ',':
                    case '%':
                        sb.Append("%" + ((uint)c).ToString("X2"));
                        break;
                    case '\r':
                        sb.Append("\n");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }

            }

            return sb.ToString();
        }
    }

    public class FormUploadElement
    {
        public string FieldName;
        public string FileName;
        public Stream FileStream;
        public string ContentType;
    }

    public class FormUploadElementCollection : Dictionary<string, FormUploadElement>
    {
        public void AddUpload(string name, FormUploadElement value)
        {
            if (this.ContainsKey(name))
            {
                this.Remove(name);
            }

            base.Add(name, value);
        }
    }

    /// <summary>
    /// Used for downloading and parsing html from the internet
    /// </summary>
    public class HtmlWeb
    {
        public enum OpenMode
        {
            Get,
            Post,
            Upload
        }

        #region Delegates

        /// <summary>
        /// Represents the method that will handle the PreHandleDocument event.
        /// </summary>
        public delegate void PreHandleDocumentHandler(HtmlDocument document);

        #endregion

        #region Fields

        /// <summary>
        /// Occurs before an HTML document is handled.
        /// </summary>
        public PreHandleDocumentHandler PreHandleDocument;

        /// <summary>
        /// Encoding used to connect
        /// </summary>
        public Encoding Encoding;
        
        /// <summary>
        /// Form elements
        /// </summary>
        public FormElementCollection FormElements { get; set; }

        public FormUploadElementCollection FormUploadElements { get; set; }

        /// <summary>
        /// Flag for generating the form elements
        /// </summary>
        public bool GenerateFormElements = false;

        public CookieContainer Cookies;

        public string LastUrl { get; private set; }
        public OpenMode LastOpenMode { get; private set; }

        public TimeSpan TempCacheExpireTime = new TimeSpan(0, 15, 0);

        #endregion

        #region Instance Methods
        
        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        public void LoadAsync(string url, OpenMode openMode = OpenMode.Get)
        {
            LoadAsync(new Uri(url), null, null, openMode);
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        /// <param name="encoding">The encoding to use while downloading the document</param>
        public void LoadAsync(string url, Encoding encoding, OpenMode openMode = OpenMode.Get)
        {
            LoadAsync(new Uri(url), encoding, null, openMode);
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        /// <param name="encoding">The encoding to use while downloading the document</param>
        /// <param name="userName">Username to use for credentials in the web request</param>
        /// <param name="password">Password to use for credentials in the web request</param>
        public void LoadAsync(string url, Encoding encoding, string userName, string password)
        {
            LoadAsync(new Uri(url), encoding, new NetworkCredential(userName, password), OpenMode.Get);
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        /// <param name="encoding">The encoding to use while downloading the document</param>
        /// <param name="userName">Username to use for credentials in the web request</param>
        /// <param name="password">Password to use for credentials in the web request</param>
        /// <param name="domain">Domain to use for credentials in the web request</param>
        public void LoadAsync(string url, Encoding encoding, string userName, string password, string domain)
        {
            LoadAsync(new Uri(url), encoding, new NetworkCredential(userName, password, domain), OpenMode.Get);
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        /// <param name="userName">Username to use for credentials in the web request</param>
        /// <param name="password">Password to use for credentials in the web request</param>
        /// <param name="domain">Domain to use for credentials in the web request</param>
        public void LoadAsync(string url, string userName, string password, string domain)
        {
            LoadAsync(new Uri(url), null, new NetworkCredential(userName, password, domain), OpenMode.Get);
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        /// <param name="userName">Username to use for credentials in the web request</param>
        /// <param name="password">Password to use for credentials in the web request</param>
        public void LoadAsync(string url, string userName, string password)
        {
            LoadAsync(new Uri(url), null, new NetworkCredential(userName, password), OpenMode.Get);
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="url">Url to the html document</param>
        /// <param name="credentials">The credentials to use for authenticating the web request</param>
        public void LoadAsync(string url, NetworkCredential credentials)
        {
            LoadAsync(new Uri(url), null, credentials, OpenMode.Get);
        }

        private string ConvertUrl(string url)
        {
            Random random = new Random();

            if (url.Contains("?"))
            {
                return url + "&random_number_to_prevent_cache=" + random.Next().ToString();
            }
            else
            {
                return url + "?random_number_to_prevent_cache=" + random.Next().ToString();
            }
        }

        /// <summary>
        /// Begins the process of downloading an internet resource
        /// </summary>
        /// <param name="uri">Url to the html document</param>
        /// <param name="encoding">The encoding to use while downloading the document</param>
        /// <param name="credentials">The credentials to use for authenticating the web request</param>
        public void LoadAsync(Uri uri, Encoding encoding, NetworkCredential credentials, OpenMode openMode = OpenMode.Get)
        {
            LastUrl = uri.AbsoluteUri;
            LastOpenMode = openMode;

            if (openMode == OpenMode.Get)
            {
                uri = new Uri(ConvertUrl(uri.AbsoluteUri));
            }

            var client = new CookieAwareClient();

            client.Cookies = Cookies;
            
            if (credentials == null)
                client.UseDefaultCredentials = true;
            else
                client.Credentials = credentials;

            if (encoding != null)
                client.Encoding = encoding;
            else if (Encoding != null)
                client.Encoding = Encoding;

            if (openMode == OpenMode.Get || (FormElements == null))
            {
                client.DownloadStringCompleted += ClientDownloadStringCompleted;
                client.DownloadStringAsync(uri);
            }
            else if (openMode == OpenMode.Post || FormUploadElements == null || FormUploadElements.Count <= 0)
            {
                string postData = FormElements.AssemblePostPayload();

                client.UploadStringCompleted += ClientUploadStringCompleted;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.Headers[HttpRequestHeader.ContentLength] = postData.Length.ToString();
                client.UploadStringAsync(uri, "POST", postData);
            }
            else
            {
                UploadFile(uri, credentials);
            }
        }

        private void OnLoadCompleted(HtmlDocumentLoadCompleted htmlDocumentLoadCompleted)
        {
            if (GenerateFormElements && (htmlDocumentLoadCompleted.Document != null))
            {
                FormElements = new FormElementCollection(htmlDocumentLoadCompleted.Document.DocumentNode);
                FormUploadElements = new FormUploadElementCollection();
            }
            else
            {
                FormElements = null;
                FormUploadElements = null;
            }

            if (LastOpenMode == OpenMode.Upload)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                        () =>
                        {
                            if (LoadCompleted != null)
                            {
                                LoadCompleted(this, htmlDocumentLoadCompleted);
                            }
                        }
                        );
            }
            else
            {
                if (LoadCompleted != null)
                {
                    LoadCompleted(this, htmlDocumentLoadCompleted);
                }
            }
        }

        private void UploadFile(Uri uri, NetworkCredential credentials)
        {
            List<MimePart> mimeParts = new List<MimePart>();

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.CookieContainer = Cookies;

                if (credentials != null)
                {
                    webRequest.Credentials = credentials;
                }
                else
                {
                    webRequest.UseDefaultCredentials = true;
                }

                foreach (var entry in FormElements)
                {
                    StringMimePart part = new StringMimePart();

                    part.Headers["Content-Disposition"] = "form-data; name=\"" + entry.Key + "\"";
                    part.StringData = entry.Value;

                    mimeParts.Add(part);
                }

                int nameIndex = 0;

                foreach (var entry in FormUploadElements)
                {
                    StreamMimePart part = new StreamMimePart();
                    var upload = entry.Value;

                    if (string.IsNullOrEmpty(upload.FieldName))
                    {
                        upload.FieldName = "file" + nameIndex++;
                    }

                    if (string.IsNullOrEmpty(upload.ContentType))
                    {
                        upload.ContentType = "application/octet-stream";
                    }

                    part.Headers["Content-Disposition"] = "form-data; name=\"" + upload.FieldName + "\"; filename=\"" + Path.GetFileName(upload.FileName) + "\"";
                    part.Headers["Content-Type"] = upload.ContentType;

                    part.SetStream(upload.FileStream);

                    mimeParts.Add(part);
                }
                
                string boundary = "----------" + DateTime.Now.Ticks.ToString("x");

                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                webRequest.Method = "POST";

                long contentLength = 0;

                byte[] _footer = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");

                foreach (MimePart part in mimeParts)
                {
                    contentLength += part.GenerateHeaderFooterData(boundary);
                }

                byte[] afterFile = Encoding.UTF8.GetBytes("\r\n");
                
                webRequest.BeginGetRequestStream(
                    asyncResult =>
                    {
                        try
                        {
                            byte[] buffer = new byte[8192];
                            int read;
                            using (Stream requestStream = webRequest.EndGetRequestStream(asyncResult))
                            {
                                foreach (MimePart part in mimeParts)
                                {
                                    requestStream.Write(part.Header, 0, part.Header.Length);

                                    while ((read = part.Data.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        requestStream.Write(buffer, 0, read);
                                    }

                                    part.Data.Dispose();
                                    
                                    requestStream.Write(afterFile, 0, afterFile.Length);
                                }

                                requestStream.Write(_footer, 0, _footer.Length);
                            }
                            
                            webRequest.BeginGetResponse(
                                asyncResult2 =>
                                {
                                    try
                                    {
                                        WebResponse webResponse = webRequest.EndGetResponse(asyncResult2);
                                        using (Stream responseStream = webResponse.GetResponseStream())
                                        {
                                            using (StreamReader sr = new StreamReader(responseStream, Encoding))
                                            {
                                                var doc = new HtmlDocument();
                                                doc.LoadHtml(sr.ReadToEnd());
                                                if (PreHandleDocument != null)
                                                    PreHandleDocument(doc);

                                                OnLoadCompleted(new HtmlDocumentLoadCompleted(doc));
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        OnLoadCompleted(new HtmlDocumentLoadCompleted(e));
                                    }
                                },
                                null);
                        }
                        catch (Exception e)
                        {
                            foreach (MimePart part in mimeParts)
                            {
                                part.Data.Dispose();
                            }

                            OnLoadCompleted(new HtmlDocumentLoadCompleted(e));
                        }

                    },
                    null
                    );

            }
            catch (Exception e)
            {
                foreach (MimePart part in mimeParts)
                {
                    part.Data.Dispose();
                }

                OnLoadCompleted(new HtmlDocumentLoadCompleted(e));
            }
        }

        #endregion

        #region Event Handling

        private void ClientDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
                OnLoadCompleted(new HtmlDocumentLoadCompleted(e.Error));
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(e.Result);
                if (PreHandleDocument != null)
                    PreHandleDocument(doc);

                OnLoadCompleted(new HtmlDocumentLoadCompleted(doc));
            }
            catch (Exception err)
            {
                OnLoadCompleted(new HtmlDocumentLoadCompleted(err));
            }
        }

        void ClientUploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
                OnLoadCompleted(new HtmlDocumentLoadCompleted(e.Error));
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(e.Result);
                if (PreHandleDocument != null)
                    PreHandleDocument(doc);

                OnLoadCompleted(new HtmlDocumentLoadCompleted(doc));
            }
            catch (Exception err)
            {
                OnLoadCompleted(new HtmlDocumentLoadCompleted(err));
            }
        }

        #endregion

        #region Event Declarations
        /// <summary>
        /// Fired when a web request has finished
        /// </summary>
        public event EventHandler<HtmlDocumentLoadCompleted> LoadCompleted;

        #endregion

        #region Public Static Methods

        public static void LoadAsync(string path, EventHandler<HtmlDocumentLoadCompleted> callback)
        {
            var web = new HtmlWeb();
            web.LoadCompleted += callback;
            web.LoadAsync(path);
        }

        #endregion
    }
}