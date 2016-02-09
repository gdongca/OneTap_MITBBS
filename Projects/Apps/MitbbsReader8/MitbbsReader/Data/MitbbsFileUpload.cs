using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class MitbbsFileUpload
    {
        public String InputPageUrl { get; protected set; }
        public String UploadPageUrl { get; protected set; }
        public long MaxUploadSize { get; protected set; }

        public List<FormUploadElement> UploadFiles { get; protected set; }

        public String AttachName { get; protected set; }

        public event EventHandler<DataLoadedEventArgs> UploadCompleted;

        public bool IsUploadPageLoaded { get; protected set; }
        public bool IsFileUploaded { get; protected set; }

        protected HtmlWeb _web;
        protected bool _uploaded;

        public MitbbsFileUpload()
        {
            MaxUploadSize = long.MaxValue;
            UploadFiles = new List<FormUploadElement>();
        }

        public virtual void ClearContent()
        {
            InputPageUrl = null;
            UploadPageUrl = null;
            AttachName = null;
            _uploaded = false;
            
            IsUploadPageLoaded = false;
            IsFileUploaded = false;
        }

        public void UploadAllFiles(HtmlWeb web, String inputPageUrl)
        {
            LoadUploadPage(web, inputPageUrl);
        }

        protected void LoadUploadPage(HtmlWeb web, String inputPageUrl)
        {
            ClearContent();
            InputPageUrl = inputPageUrl;
            _web = web;

            _web.LoadCompleted += OnUploadPageLoaded;
            _web.LoadAsync(InputPageUrl);
        }

        protected void StartUploadNextFile()
        {
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (IsUploadPageLoaded && UploadPageUrl != null && UploadFiles.Count > 0)
            {
                PopulateForm();

                _uploaded = true;

                _web.LoadCompleted += OnUploadPageLoaded;
                _web.LoadAsync(UploadPageUrl, HtmlWeb.OpenMode.Upload);
            }
            else
            {
                TriggerUploadCompleted(loadedArgs);
            }
        }

        protected void OnUploadPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnUploadPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    if (VerifyUpload(args.Document.DocumentNode))
                    {
                        ExtractForm(args.Document.DocumentNode);

                        if (UploadFiles.Count > 0)
                        {
                            StartUploadNextFile();
                            return;
                        }
                        else
                        {
                            IsFileUploaded = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    loadedArgs.Error = e;
                }
            }
            else
            {
                loadedArgs.Error = args.Error;
            }

            TriggerUploadCompleted(loadedArgs);
        }

        protected void TriggerUploadCompleted(DataLoadedEventArgs args)
        {
            if (UploadCompleted != null)
            {
                UploadCompleted(this, args);
            }
        }

        protected bool VerifyUpload(HtmlNode documentNode)
        {
            if (!_uploaded)
            {
                return true;
            }

            bool success = false;
            IEnumerable<HtmlNode> textNodes = documentNode.Descendants("#text");

            foreach (HtmlNode textNode in textNodes)
            {
                String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                if (
                    text.Contains("上载成功")
                    )
                {
                    success = true;
                }
                else
                {
                    String findStr = "opener.document.forms[\"postform\"].elements[\"attachname\"].value = \"";
                    int index1 = text.IndexOf(findStr);
                    if (index1 >= 0)
                    {
                        index1 += findStr.Length;
                        string temp = text.Substring(index1);

                        int index2 = temp.IndexOf("\"");
                        if (index2 >= 0)
                        {
                            AttachName = temp.Substring(0, index2);

                            if (success)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return success;
        }

        protected void ExtractForm(HtmlNode documentNode)
        {
            var postFormNodes = from formNode in documentNode.Descendants("form")
                                where formNode.Attributes.Contains("name") && formNode.Attributes["name"].Value == "addattach"
                                select formNode;

            foreach (HtmlNode postFormNode in postFormNodes)
            {
                HtmlNode postFormParent = postFormNode.ParentNode;
                UploadPageUrl = HtmlUtilities.GetAbsoluteUrl(InputPageUrl, postFormNode.Attributes["action"].Value);

                _web.FormElements = new FormElementCollection(postFormParent);
                _web.FormElements["act"] = "add";

                if (_web.FormElements.ContainsKey("MAX_FILE_SIZE"))
                {
                    long maxSize;
                    if (long.TryParse(_web.FormElements["MAX_FILE_SIZE"], out maxSize))
                    {
                        MaxUploadSize = maxSize;
                    }
                }

                IsUploadPageLoaded = true;

                break;
            }
        }

        protected void PopulateForm()
        {
            _web.FormElements.Remove("attachfile");
            _web.FormUploadElements = new FormUploadElementCollection();

            var upload = UploadFiles[0];
            upload.FieldName = "attachfile";
            _web.FormUploadElements.AddUpload(upload.FieldName, upload);

            UploadFiles.RemoveAt(0);
        }
    }
}
