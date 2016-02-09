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
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class MitbbsPostForward
    {
        public enum ForwardMode
        {
            Board,
            Club
        }

        public String InputPageUrl { get; protected set; }
        public String ForwardPageUrl { get; protected set; }
        public String VerifyImageUrl { get; protected set; }
        public String VerifyCode { get; set; }

        public event EventHandler<DataLoadedEventArgs> InputPageLoaded;
        public event EventHandler<DataLoadedEventArgs> ForwardCompleted;

        public bool IsInputPageLoaded { get; protected set; }
        public bool IsPostForwarded { get; protected set; }

        public String BoardName { get; set; }
        public ForwardMode Mode { get; set; }

        protected HtmlWeb _web;

        public virtual void ClearContent()
        {
            InputPageUrl = null;
            ForwardPageUrl = null;
            VerifyImageUrl = null;
            VerifyCode = null;

            BoardName = null;
            Mode = ForwardMode.Board;

            IsInputPageLoaded = false;
            IsPostForwarded = false;
        }

        public void LoadInputPage(HtmlWeb web, String inputPageUrl)
        {
            ClearContent();
            InputPageUrl = inputPageUrl;
            _web = web;

            _web.LoadCompleted += OnInputPageLoaded;
            _web.LoadAsync(InputPageUrl);
        }

        public void ForwardPost()
        {
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (IsInputPageLoaded && (ForwardPageUrl != null))
            {
                PopulateForm();

                _web.LoadCompleted += OnForwardPageLoaded;
                _web.LoadAsync(ForwardPageUrl, HtmlWeb.OpenMode.Post);
            }
            else
            {
                if (ForwardCompleted != null)
                {
                    ForwardCompleted(this, loadedArgs);
                }
            }
        }

        protected void OnInputPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnInputPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
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

            if (InputPageLoaded != null)
            {
                InputPageLoaded(this, loadedArgs);
            }
        }

        protected void OnForwardPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnForwardPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    VerifyForward(args.Document.DocumentNode);
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

            if (ForwardCompleted != null)
            {
                ForwardCompleted(this, loadedArgs);
            }
        }

        protected void ExtractForm(HtmlNode documentNode)
        {
            var postFormNodes = from formNode in documentNode.Descendants("form")
                                where formNode.Attributes.Contains("name") && formNode.Attributes["name"].Value == "postform"
                                select formNode;

            foreach (HtmlNode postFormNode in postFormNodes)
            {
                HtmlNode postFormParent = postFormNode.ParentNode;
                ForwardPageUrl = HtmlUtilities.GetAbsoluteUrl(InputPageUrl, postFormNode.Attributes["action"].Value);

                _web.FormElements = new FormElementCollection(postFormParent);

                IEnumerable<HtmlNode> imageNodes = postFormParent.Descendants("img");
                foreach (HtmlNode imageNode in imageNodes)
                {
                    VerifyImageUrl = HtmlUtilities.GetAbsoluteUrl(InputPageUrl, imageNode.Attributes["src"].Value);
                    break;
                }

                if (_web.FormElements.ContainsKey("target"))
                {
                    BoardName = _web.FormElements["target"];
                }

                if (_web.FormElements.ContainsKey("mode"))
                {
                    String modeStr = _web.FormElements["mode"];

                    if (modeStr == "1")
                    {
                        Mode = ForwardMode.Club;
                    }
                    else
                    {
                        Mode = ForwardMode.Board;
                    }
                }

                if (_web.FormElements.ContainsKey("validcode"))
                {
                    VerifyCode = _web.FormElements["validcode"];
                }

                IsInputPageLoaded = true;
                break;
            }
        }

        protected void PopulateForm()
        {
            _web.FormElements["target"] = BoardName;

            if (VerifyCode != null)
            {
                _web.FormElements["validcode"] = VerifyCode;
            }

            if (Mode == ForwardMode.Club)
            {
                _web.FormElements["mode"] = "1";
            }
            else
            {
                _web.FormElements["mode"] = "0";
            }
        }

        protected void VerifyForward(HtmlNode documentNode)
        {
            IEnumerable<HtmlNode> textNodes = documentNode.Descendants("#text");

            foreach (HtmlNode textNode in textNodes)
            {
                String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                if (
                   text.Contains("已转贴到")
                   )
                {
                    IsPostForwarded = true;
                    break;
                }
            }
        }
    }
}
