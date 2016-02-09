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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public abstract class MitbbsPostEditBase
    {
        public String EditPageUrl { get; protected set; }
        public String SendPageUrl { get; protected set; }
        public String VerifyImageUrl { get; protected set; }
        public String VerifyCode { get; set; }
        public String UploadFileUrl { get; protected set; }
        public long MaxUploadSize { get; protected set; }

        public event EventHandler<DataLoadedEventArgs> EditPageLoaded;
        public event EventHandler<DataLoadedEventArgs> SendPostCompleted;

        public bool IsEditPageLoaded { get; protected set; }
        public bool IsPostSent { get; protected set; }
        public bool IsUploadFailed { get; protected set; }
        
        public String Recipient { get; set; }
        public String PostTitle { get; set; }
        public String PostBody { get; set; }

        public ObservableCollection<FormUploadElement> UploadFiles { get; protected set; }

        protected HtmlWeb _web;

        public MitbbsPostEditBase()
        {
            UploadFiles = new ObservableCollection<FormUploadElement>();

            MaxUploadSize = long.MaxValue;
        }
        
        public void LoadEditPage(HtmlWeb web, String editPageUrl)
        {
            ClearContent();
            EditPageUrl = editPageUrl;
            _web = web;

            _web.LoadCompleted += OnEditPageLoaded;
            _web.LoadAsync(EditPageUrl);
        }

        public virtual void SendPost()
        {
            SendPostInternal();
        }

        protected void SendPostInternal()
        {
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (IsEditPageLoaded && (SendPageUrl != null))
            {
                PopulateForm();
                
                _web.LoadCompleted += OnSendPageLoaded;
                _web.LoadAsync(SendPageUrl, HtmlWeb.OpenMode.Post);
            }
            else
            {
                TriggerCompletedEvent(loadedArgs);
            }
        }

        public virtual void ClearContent()
        {
            SendPageUrl = null;
            VerifyImageUrl = null;
            UploadFileUrl = null;
            VerifyCode = null;
            Recipient = null;
            
            PostTitle = "";
            PostBody = "";
            
            IsEditPageLoaded = false;
            IsPostSent = false;
            IsUploadFailed = false;
        }

        protected void OnEditPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnEditPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if(args.Document != null)
            {
                try
                {
                    ExtractForm(args.Document.DocumentNode);
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

            if (EditPageLoaded != null)
            {
                EditPageLoaded(this, loadedArgs);
            }
        }

        protected void OnSendPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnSendPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    VerifySend(args.Document.DocumentNode);
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

            TriggerCompletedEvent(loadedArgs);
        }

        protected void TriggerCompletedEvent(DataLoadedEventArgs args)
        {
            if (SendPostCompleted != null)
            {
                SendPostCompleted(this, args);
            }
        }

        public static void GenerateReplyTemplate(MitbbsPostBase post, out String title, out String body)
        {
            if (post.Title.StartsWith("Re:"))
            {
                title = post.Title;
            }
            else
            {
                title = "Re:" + post.Title;
            }

            body = "\n\n【 在 " + post.Author + " 的大作中提到: 】\n";

            int lineCount = 0;
            for (int i = 0; (i < post.Contents.Count) && (lineCount < 1); i++)
            {
                ContentBlock content = post.Contents[i];
                if (content is TextContentBlock)
                {
                    body += ":" + (content as TextContentBlock).Text.Trim('\n') + "\n";
                    lineCount++;
                }
            }

            if (body.Length > 80)
            {
                body = body.Substring(0, 80);
            }
        }

        protected abstract void PopulateForm();
        protected abstract void ExtractForm(HtmlNode documentNode);
        protected abstract void VerifySend(HtmlNode documentNode);

    }

    public class MitbbsPostEditMobile : MitbbsPostEditBase
    {
        public static String MailEditPageUrl = "/mobile/mitbbs_mailbox.php";

        protected override void PopulateForm()
        {
            _web.FormElements["title"] = PostTitle;
            _web.FormElements["text"] = PostBody;

            if (VerifyCode != null)
            {
                _web.FormElements["validcode"] = VerifyCode;
            }

            if (Recipient != null)
            {
                _web.FormElements["userid"] = Recipient;
            }

            if (_web.FormElements.ContainsKey("backup"))
            {
                _web.FormElements["backup"] = "on";
            }
        }

        protected override void ExtractForm(HtmlNode documentNode)
        {
            IEnumerable<HtmlNode> divNodes = documentNode.Descendants("div");

            foreach (HtmlNode divNode in divNodes)
            {
                if (divNode.Attributes.Contains("id"))
                {
                    if (divNode.Attributes["id"].Value.StartsWith("userandpass"))
                    {
                        _web.FormElements = new FormElementCollection(divNode);

                        IEnumerable<HtmlNode> imageNodes = divNode.Descendants("img");
                        foreach (HtmlNode imageNode in imageNodes)
                        {
                            VerifyImageUrl = HtmlUtilities.GetAbsoluteUrl(EditPageUrl, imageNode.Attributes["src"].Value);
                            break;
                        }

                        IEnumerable<HtmlNode> formNodes = divNode.Descendants("form");

                        foreach (HtmlNode formNode in formNodes)
                        {
                            //if ((formNode.Attributes["name"].Value == "postform") || (formNode.Attributes["name"].Value == "post"))
                            {
                                SendPageUrl = HtmlUtilities.GetAbsoluteUrl(EditPageUrl, formNode.Attributes["action"].Value);

                                //VerifyImageUrl = HtmlUtilities.GetAbsoluteUrl(EditPageUrl, "/img_rand/img_rand.php");

                                if (_web.FormElements.ContainsKey("title"))
                                {
                                    PostTitle = _web.FormElements["title"];
                                }

                                if (_web.FormElements.ContainsKey("text"))
                                {
                                    PostBody = _web.FormElements["text"];
                                }

                                if (_web.FormElements.ContainsKey("validcode"))
                                {
                                    VerifyCode = _web.FormElements["validcode"];
                                }

                                if (_web.FormElements.ContainsKey("userid"))
                                {
                                    Recipient = _web.FormElements["userid"];
                                }
                                else
                                {
                                    Recipient = null;
                                }

                                IsEditPageLoaded = true;
                                break;
                            }
                        }

                        break;
                    }
                }
            }

        }

        protected override void VerifySend(HtmlNode documentNode)
        {
            IEnumerable<HtmlNode> textNodes = documentNode.Descendants("#text");

            foreach (HtmlNode textNode in textNodes)
            {
                String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                if (
                    text.Contains("发文成功") ||
                    text.Contains("回复文章成功") ||
                    text.Contains("修改文章成功") ||
                    text.Contains("信件已成功发送")
                    )
                {
                    IsPostSent = true;
                    break;
                }
            }

            if (documentNode.FirstChild == null)
            {
                // For reason the Html class can't parse the send post page
                // so assume it is successful if we got an empty document node
                //
                IsPostSent = true;
            }
        }
    }

    public class MitbbsPostEdit : MitbbsPostEditBase
    {
        protected MitbbsFileUpload _fileUpload = new MitbbsFileUpload();
        protected String _attachName = null;

        public override void SendPost()
        {
            if (UploadFileUrl != null && UploadFiles.Count > 0)
            {
                _fileUpload.UploadFiles.Clear();

                foreach (var upload in UploadFiles)
                {
                    String extName = System.IO.Path.GetExtension(upload.FileName).ToLower();
                    if (extName.EndsWith("jpg") || extName.EndsWith("jpeg"))
                    {
                        upload.ContentType = "image/jpeg";
                    }

                    upload.FileStream.Seek(0, System.IO.SeekOrigin.Begin);

                    _fileUpload.UploadFiles.Add(upload);
                }

                _fileUpload.UploadCompleted += OnUploadCompleted;
                _fileUpload.UploadAllFiles(_web, UploadFileUrl);

                UploadFiles.Clear();
            }
            else
            {
                base.SendPost();
            }
        }

        public override void ClearContent()
        {
            base.ClearContent();
            _attachName = null;
        }

        protected void OnUploadCompleted(object sender, DataLoadedEventArgs args)
        {
            _fileUpload.UploadCompleted -= OnUploadCompleted;

            if (_fileUpload.IsFileUploaded)
            {
                MaxUploadSize = _fileUpload.MaxUploadSize;
                _attachName = _fileUpload.AttachName;
                SendPostInternal();
            }
            else
            {
                IsUploadFailed = true;
                TriggerCompletedEvent(new DataLoadedEventArgs());
            }
        }

        protected override void PopulateForm()
        {
            _web.FormElements["title"] = PostTitle;
            _web.FormElements["text"] = PostBody;

            if (VerifyCode != null)
            {
                _web.FormElements["validcode"] = VerifyCode;
            }

            if (Recipient != null)
            {
                _web.FormElements["userid"] = Recipient;
            }

            if (_web.FormElements.ContainsKey("backup"))
            {
                _web.FormElements["backup"] = "on";
            }

            if (_web.FormElements.ContainsKey("sendtoblog_flag"))
            {
                _web.FormElements["sendtoblog_flag"] = "off";
            }

            if (_attachName != null)
            {
                _web.FormElements["attachname"] = _attachName;
            }
        }

        protected override void ExtractForm(HtmlNode documentNode)
        {
            var postFormNodes = from formNode in documentNode.Descendants("form")
                                where formNode.Attributes.Contains("name") && (formNode.Attributes["name"].Value == "postform" || formNode.Attributes["name"].Value == "form1")
                                select formNode;

            foreach (HtmlNode postFormNode in postFormNodes)
            {
                HtmlNode postFormParent = postFormNode.ParentNode;
                SendPageUrl = HtmlUtilities.GetAbsoluteUrl(EditPageUrl, postFormNode.Attributes["action"].Value);

                _web.FormElements = new FormElementCollection(postFormParent);

                IEnumerable<HtmlNode> imageNodes = postFormParent.Descendants("img");
                foreach (HtmlNode imageNode in imageNodes)
                {
                    VerifyImageUrl = HtmlUtilities.GetAbsoluteUrl(EditPageUrl, imageNode.Attributes["src"].Value);
                    break;
                }

                if (_web.FormElements.ContainsKey("title"))
                {
                    PostTitle = _web.FormElements["title"];
                }

                if (_web.FormElements.ContainsKey("text"))
                {
                    PostBody = _web.FormElements["text"];
                }

                if (_web.FormElements.ContainsKey("validcode"))
                {
                    VerifyCode = _web.FormElements["validcode"];
                }

                if (_web.FormElements.ContainsKey("userid"))
                {
                    Recipient = _web.FormElements["userid"];
                }
                else
                {
                    Recipient = null;
                }

                if (_web.FormElements.ContainsKey("attachname"))
                {
                    UploadFileUrl = HtmlUtilities.GetAbsoluteUrl(EditPageUrl, "bbsupload.php");
                }
                else
                {
                    UploadFileUrl = null;
                }

                IsEditPageLoaded = true;
                break;
            }
        }

        protected override void VerifySend(HtmlNode documentNode)
        {
            IEnumerable<HtmlNode> textNodes = documentNode.Descendants("#text");

            foreach (HtmlNode textNode in textNodes)
            {
                String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                if (
                    text.Contains("发文成功") ||
                    text.Contains("回复文章成功") ||
                    text.Contains("修改文章成功") ||
                    text.Contains("信件已成功发送")
                    )
                {
                    IsPostSent = true;
                    break;
                }
            }

            if (documentNode.FirstChild == null)
            {
                // For reason the Html class can't parse the send post page
                // so assume it is successful if we got an empty document node
                //
                IsPostSent = true;
            }
        }
    }
}
