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
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class MitbbsTopicEssenceMobile : MitbbsTopicBase
    {
        public override void LoadFromUrl(HtmlWeb web, String url, int pageToLoad = -1)
        {
            Url = url;
            IsLoaded = false;

            _web = web;
            _web.LoadCompleted += OnUrlLoaded;
            _web.LoadAsync(url);
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                ClearContent();

                CalculateBoardUrl();

                IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");

                foreach (HtmlNode divNode in divNodes)
                {
                    if ((divNode.Attributes["id"] != null) && (divNode.Attributes["id"].Value == "wenzhangyudu"))
                    {
                        HtmlNode postNode = divNode;
                        MitbbsPostBase post = new MitbbsEssencePostMobile();
                        post.ParentUrl = Url;
                        if (post.LoadFromHtml(postNode))
                        {
                            if (Posts.Count <= 0)
                            {
                                Title = post.Title + " (精华区)";
                                BoardName = post.BoardName;
                                FirstAuthor = post.Author;
                                ReplyUrl = post.ReplyPostUrl;

                                IsLoaded = true;
                            }

                            Posts.Add(post);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
                IsLoaded = false;
            }

            return IsLoaded;
        }

        private String _topicUrlTemplate = "(?<1>.*)?path=(?<2>.*).faq/(?<3>.*)/(?<4>.*)";
        private void CalculateBoardUrl()
        {
            Match match = Regex.Match(Url, _topicUrlTemplate);
            if (match.Success)
            {
                String boardId = match.Groups[3].Value;
                int findIndex = boardId.IndexOf("/");
                if (findIndex >= 0)
                {
                    boardId = boardId.Substring(0, findIndex);
                }

                String boardUrl = "mbbsdoc.php?board=" + boardId;

                BoardUrl = HtmlUtilities.GetAbsoluteUrl(Url, boardUrl);
            }
            else
            {
                BoardUrl = null;
            }
        }
    }
}
