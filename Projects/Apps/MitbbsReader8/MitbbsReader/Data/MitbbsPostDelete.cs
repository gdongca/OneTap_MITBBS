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
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class MitbbsPostDeleteMobile
    {
        public String ConfirmPageUrl { get; private set; }
        public String DeletePageUrl { get; private set; }

        public event EventHandler<DataLoadedEventArgs> DeletePostCompleted;

        public bool IsConfirmPageLoaded { get; private set; }
        public bool IsPostDeleted { get; private set; }

        private HtmlWeb _web;

        public void DeletePost(HtmlWeb web, String confirmPageUrl)
        {
            ClearContent();
            ConfirmPageUrl = confirmPageUrl;
            _web = web;

            _web.LoadCompleted += OnConfirmPageLoaded;
            _web.LoadAsync(ConfirmPageUrl);
        }

        private void ConfirmDelete()
        {
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (IsConfirmPageLoaded && (DeletePageUrl != null))
            {
                _web.LoadCompleted += OnDeletePageLoaded;
                _web.LoadAsync(DeletePageUrl, HtmlWeb.OpenMode.Post);
            }
            else
            {
                if (DeletePostCompleted != null)
                {
                    DeletePostCompleted(this, loadedArgs);
                }
            }
        }

        public void ClearContent()
        {
            DeletePageUrl = null;

            IsConfirmPageLoaded = false;
            IsPostDeleted = false;
        }

        private void OnConfirmPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnConfirmPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    IEnumerable<HtmlNode> divNodes = args.Document.DocumentNode.Descendants("div");

                    foreach (HtmlNode divNode in divNodes)
                    {
                        if (divNode.Attributes["id"].Value == "addlink")
                        {
                            _web.FormElements = new FormElementCollection(divNode);

                            IEnumerable<HtmlNode> formNodes = divNode.Descendants("form");

                            foreach (HtmlNode formNode in formNodes)
                            {
                                //if (formNode.Attributes["name"].Value == "delform")
                                {
                                    DeletePageUrl = HtmlUtilities.GetAbsoluteUrl(ConfirmPageUrl, formNode.Attributes["action"].Value);

                                    IsConfirmPageLoaded = true;
                                    break;
                                }
                            }

                            break;
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


            if (IsConfirmPageLoaded)
            {
                ConfirmDelete();
            }
            else
            {
                if (DeletePostCompleted != null)
                {
                    DeletePostCompleted(this, loadedArgs);
                }
            }
        }

        private void OnDeletePageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnDeletePageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    IEnumerable<HtmlNode> textNodes = args.Document.DocumentNode.Descendants("#text");
                    foreach (HtmlNode textNode in textNodes)
                    {
                        String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                        if (text.Contains("删除成功") ||
                            text.Contains("进入我的信箱"))
                        {
                            IsPostDeleted = true;
                            break;
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

            if (DeletePostCompleted != null)
            {
                DeletePostCompleted(this, loadedArgs);
            }
        }
    }

    public class MitbbsPostDelete
    {
        public String File { get; set; }
        public String ID { get; set; }
        public String DingFlag { get; set; }
        
        public bool IsInited { get; private set; }
        public bool IsPostDeleted { get; private set; }

        public event EventHandler<DataLoadedEventArgs> DeletePostCompleted;

        private HtmlWeb _web;
        private String _submitUrl;

        public MitbbsPostDelete()
        {
            IsInited = false;
            IsPostDeleted = false;
        }

        public void Initialize(HtmlWeb web, HtmlNode delForm, String topicPageUrl)
        {
            IsInited = false;
            IsPostDeleted = false;

            _web = web;
            HtmlNode parentNode = delForm.ParentNode;
            _web.FormElements = new FormElementCollection(parentNode);

            _submitUrl = HtmlUtilities.GetAbsoluteUrl(topicPageUrl, delForm.Attributes["action"].Value);

            File = "";
            ID = "";
            DingFlag = "";
            IsInited = true;
        }

        public void DeletePost(String delLinkParameters)
        {
            IsPostDeleted = false;

            if (!IsInited || !ParseDelLinkParameters(delLinkParameters))
            {
                if (DeletePostCompleted != null)
                {
                    DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();
                    DeletePostCompleted(this, loadedArgs);
                }

                return;
            }

            _web.FormElements["file"] = File;
            _web.FormElements["id"] = ID;
            _web.FormElements["dingflag"] = DingFlag;

            _web.LoadCompleted += OnDeletePageLoaded;
            _web.LoadAsync(_submitUrl, HtmlWeb.OpenMode.Post);
        }

        private bool ParseDelLinkParameters(String delLinkParameters)
        {
            const String delLinkTemplate = @"return del_article\('(?<1>.*)',(?<2>.*),(?<3>.*)\);";

            Match match = Regex.Match(delLinkParameters, delLinkTemplate);
            if (!match.Success)
            {
                return false;
            }

            File = match.Groups[1].Value;
            ID = match.Groups[2].Value;
            DingFlag = match.Groups[3].Value;

            return true;
        }

        private void OnDeletePageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnDeletePageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    IEnumerable<HtmlNode> textNodes = args.Document.DocumentNode.Descendants("#text");
                    foreach (HtmlNode textNode in textNodes)
                    {
                        String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                        if (text.Contains("删除成功") ||
                            text.Contains("进入我的信箱"))
                        {
                            IsPostDeleted = true;
                            break;
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

            if (DeletePostCompleted != null)
            {
                DeletePostCompleted(this, loadedArgs);
            }
        }
    }
}
