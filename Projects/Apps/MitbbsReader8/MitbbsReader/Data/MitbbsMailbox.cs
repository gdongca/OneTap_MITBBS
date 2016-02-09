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
    public class MitbbsMailbox
    {
        public static String InboxUrl = "/mobile/mitbbs_mailbox.php?option=receive&path=r";
        public static String OutboxUrl = "/mobile/mitbbs_mailbox.php?option=send&path=s";
        public static String NewMailsUrl = "/mobile/mitbbs_mailbox.php?option=new";
        public static String CreateMailUrl = "/mobile/mitbbs_mailbox.php";

        public String Url { get; private set; }
        public String MailboxName { get; set; }

        public String FirstPageUrl { get; private set; }
        public String LastPageUrl { get; private set; }
        public String PrevPageUrl { get; private set; }
        public String NextPageUrl { get; private set; }

        public String NewMailUrl { get; private set; }
        public bool HasNewMail { get; private set; }

        public ObservableCollection<MitbbsMailLink> MailLinks { get; private set; }

        public bool IsLoaded { get; private set; }

        public event EventHandler<DataLoadedEventArgs> MailboxLoaded;

        private HtmlWeb _web;

        public MitbbsMailbox()
        {
            MailLinks = new ObservableCollection<MitbbsMailLink>();
            MailboxName = "邮箱";
            ClearContent();
        }

        public void LoadFromUrl(HtmlWeb web, String url)
        {
            ClearContent();
            Url = url;
            _web = web;
            
            _web.LoadCompleted += OnUrlLoaded;
            _web.LoadAsync(url);
        }

        public void ClearContent()
        {
            FirstPageUrl = null;
            LastPageUrl = null;
            PrevPageUrl = null;
            NextPageUrl = null;
            NewMailUrl = null;
            HasNewMail = false;

            MailLinks.Clear();
            
            IsLoaded = false;
        }

        private void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnUrlLoaded;
            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();
            loadedEventArgs.Error = args.Error;

            if (args.Document != null)
            {
                try
                {
                    LoadFromHtml(args.Document.DocumentNode);
                }
                catch (Exception e)
                {
                    loadedEventArgs.Error = e;
                }
            }

            if (MailboxLoaded != null)
            {
                MailboxLoaded(this, loadedEventArgs);
            }
        }

        private bool LoadFromHtml(HtmlNode rootNode)
        {
            bool loaded = false;
            ClearContent();

            bool pageLinksAreReaded = false;
            IEnumerable<HtmlNode> divNodes = rootNode.Descendants("div");
            foreach (HtmlNode divNode in divNodes)
            {
                if (divNode.Attributes["id"] != null)
                {
                    if (divNode.Attributes["id"].Value == "addlink")
                    {
                        IEnumerable<HtmlNode> linkNodes = divNode.Descendants("a");
                        foreach (HtmlNode linkNode in linkNodes)
                        {
                            String link = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                            String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText).Trim();

                            if (linkText == "写邮件")
                            {
                                NewMailUrl = link;
                                loaded = true;
                            }
                        }
                    }
                    else if (!pageLinksAreReaded && (divNode.Attributes["id"].Value == "turnpage"))
                    {
                        IEnumerable<HtmlNode> linkNodes = divNode.Descendants("a");
                        foreach (HtmlNode linkNode in linkNodes)
                        {
                            String link = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                            String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText).Trim();

                            if (linkText == "[首页]")
                            {
                                FirstPageUrl = link;
                            }
                            else if (linkText == "[上页]")
                            {
                                PrevPageUrl = link;
                            }
                            else if (linkText == "[下页]")
                            {
                                NextPageUrl = link;
                            }
                            else if (linkText == "[末页]")
                            {
                                LastPageUrl = link;
                            }
                        }

                        pageLinksAreReaded = true;
                    }
                    else if (divNode.Attributes["id"].Value == "wenzhangyudu")
                    {
                        IEnumerable<HtmlNode> liNodes = divNode.Descendants("li");
                        foreach (HtmlNode liNode in liNodes)
                        {
                            MitbbsMailLink mailLink = new MitbbsMailLink();
                            mailLink.ParentUrl = Url;

                            if (mailLink.LoadFromHtml(liNode))
                            {
                                MailLinks.Add(mailLink);
                                if (mailLink.IsNew)
                                {
                                    HasNewMail = true;
                                }
                            }
                        }
                    }
                }
            }

            IsLoaded = loaded;

            return IsLoaded;
        }
    }
}
