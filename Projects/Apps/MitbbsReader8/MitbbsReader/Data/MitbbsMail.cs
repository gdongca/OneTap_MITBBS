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
    public class MitbbsMail : MitbbsTopicBase
    {
        public String DeleteUrl { get; set; }

        public override void ClearContent()
        {
            base.ClearContent();

            DeleteUrl = null;
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            ClearContent();

            MitbbsMailPostMobile post = new MitbbsMailPostMobile();
            post.ParentUrl = Url;
            post.IgnoreMissingHeader = true;

            IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");

            foreach (HtmlNode divNode in divNodes)
            {
                if (divNode.Attributes.Contains("id"))
                {
                    if (divNode.Attributes["id"].Value == "wenzhangyudu")
                    {
                        if (divNode.ChildNodes.Count >= 2 && divNode.ChildNodes[1].Name == "p")
                        {
                            if (post.LoadMailFooterFromHtml(divNode))
                            {
                                ReplyUrl = post.ReplyPostUrl;
                                DeleteUrl = post.DeletePostUrl;
                            }
                        }
                        else if (post.LoadMailBodyFromHtml(divNode))
                        {
                            if (Posts.Count <= 0)
                            {
                                Title = post.Title;
                                FirstAuthor = post.Author;
                                
                                Posts.Add(post);

                                IsLoaded = true;
                            }
                        }
                    }
                }
            }

            return IsLoaded;
        }
    }

}
