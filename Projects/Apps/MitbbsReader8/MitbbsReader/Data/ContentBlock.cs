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
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    [XmlInclude(typeof(TextContentBlock))]
    [XmlInclude(typeof(ImageBlock))]
    [XmlInclude(typeof(LinkBlock))]
    [XmlInclude(typeof(VideoBlock))]
    [XmlInclude(typeof(QuoteBlock))]
    public abstract class ContentBlock
    {
        public bool NoMerge { get; set; }
        public abstract bool LoadFromHtml(HtmlNode rootNode);
        public abstract bool MergeWith(ContentBlock contentBlock);

        public ContentBlock()
        {
            NoMerge = false;
        }

        public static ContentBlock CreateContentBlockFromHtml(HtmlNode rootNode, String parentUrl)
        {
            ContentBlock contentBlock = null;

            contentBlock = new QuoteBlock();
            if (contentBlock.LoadFromHtml(rootNode))
            {
                return contentBlock;
            }

            contentBlock = new TextContentBlock();
            if (contentBlock.LoadFromHtml(rootNode))
            {
                return contentBlock;
            }

            contentBlock = new ImageBlock();
            (contentBlock as ImageBlock).ParentUrl = parentUrl;
            if (contentBlock.LoadFromHtml(rootNode))
            {
                return contentBlock;
            }

            contentBlock = new LinkBlock();
            if (contentBlock.LoadFromHtml(rootNode))
            {
                return contentBlock;
            }

            contentBlock = new VideoBlock();
            if (contentBlock.LoadFromHtml(rootNode))
            {
                return contentBlock;
            }

            return null;
        }
    }

    public class TextContentBlock : ContentBlock
    {
        public String Text { get; set; }

        private int _maxTextBlockSize = 500;
        
        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            if (rootNode.Name == "#text")
            {
                Text = HtmlUtilities.GetPlainHtmlText(rootNode.InnerText);
                return true;
            }
            else if(rootNode.Name == "br")
            {
                Text = "\n";
                return true;
            }

            return false;
        }

        public override bool MergeWith(ContentBlock contentBlock)
        {
            String newText = null;
            if (contentBlock is TextContentBlock)
            {
                newText = (contentBlock as TextContentBlock).Text;

                if (newText == "")
                {
                    return true;
                }
            }

            if (!NoMerge && !contentBlock.NoMerge)
            {
                if (!Text.EndsWith("\n\n") && (contentBlock is TextContentBlock) && !(contentBlock is QuoteBlock))
                {
                    if ((Text.Length + newText.Length) <= _maxTextBlockSize)
                    {
                        if ((newText != "\n") && (newText != ""))
                        {
                            if ((Text.Length > 1) && Text.EndsWith("\n") && !Text.EndsWith("\n\n"))
                            {
                                Text = Text.Substring(0, Text.Length - 1);
                                Text = Text + " ";
                            }
                        }

                        //if (!Text.EndsWith("\n") || (newText != "\n"))
                        {
                            Text = Text + newText;
                            return true;
                        }
                    }
                }

                if (contentBlock is TextContentBlock)
                {
                    if (Text.EndsWith("\n") && (newText == "\n"))
                    {
                        return true;
                    }
                }
            }

            if (contentBlock is TextContentBlock)
            {
                if ((NoMerge || contentBlock.NoMerge) && (newText == "\n"))
                {
                    return true;
                }
            }

            while (Text.EndsWith("\n\n") && Text.Length > 1)
            {
                Text = Text.Substring(0, Text.Length - 1);
            }

            return false;
        }
    }

    public class ImageBlock : ContentBlock
    {
        public String ParentUrl { get; set; }
        public String ImageUrl { get; set; }

        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            if (rootNode.Name == "img")
            {
                String url = rootNode.Attributes["src"].Value;
                if (!IsUrlExcluded(url))
                {
                    ImageUrl = url;
                    return true;
                }
            }
            else if (rootNode.Name == "a")
            {
                for (int i = 0; i < rootNode.ChildNodes.Count; i++)
                {
                    HtmlNode node = rootNode.ChildNodes[i];

                    if (node.Name == "img")
                    {
                        String url = node.Attributes["src"].Value;
                        if (!IsUrlExcluded(url))
                        {
                            ImageUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, url);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool MergeWith(ContentBlock contentBlock)
        {
            return false;
        }

        private String [] _excludedImageUrls = {
            "/images/files/img.gif"
        };

        private bool IsUrlExcluded(String imageUrl)
        {
            for (int i = 0; i < _excludedImageUrls.Length; i++)
            {
                if (imageUrl.ToLower() == _excludedImageUrls[i])
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class LinkBlock : ContentBlock
    {
        public String Text { get; set; }
        public String Url { get; set; }
        
        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            if (rootNode.Name == "a")
            {
                Text = rootNode.InnerText;
                Url = rootNode.Attributes["href"].Value;
                return true;
            }
            
            return false;
        }

        public override bool MergeWith(ContentBlock contentBlock)
        {
            return false;
        }
    }

    public class VideoBlock : ContentBlock
    {
        public String VideoLink { get; set; }
        public String OriginalSource { get; set; }

        private String _youtubeUrlTemplate1 = "(?<1>.*)youtube.com(?<4>.*)/v/(?<2>.*)&(?<3>.*)";
        private String _youtubeUrlTemplate2 = "(?<1>.*)youtube.com(?<3>.*)/v/(?<2>.*)";

        private String _youkuUrlTemplate1 = "(?<1>.*)youku.com(?<2>.*)/sid/(?<3>.*)";
        private String _youku3GUrl = "http://3g.youku.com/wap2/video.jsp?vid={0}";

        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            if (rootNode.Name == "object")
            {
                IEnumerable<HtmlNode> paramNodes = rootNode.Descendants("param");
                foreach (HtmlNode paramNode in paramNodes)
                {
                    if ((paramNode.Attributes["name"] != null) &&
                        (paramNode.Attributes["name"].Value.ToLower() == "movie") &&
                        (paramNode.Attributes["value"] != null))
                    {
                        String url = paramNode.Attributes["value"].Value;
                        OriginalSource = url;

                        Match match = Regex.Match(url, _youtubeUrlTemplate1);

                        if (!match.Success)
                        {
                            match = Regex.Match(url, _youtubeUrlTemplate2);
                        }

                        if (match.Success)
                        {
                            VideoLink = "vnd.youtube:" + match.Groups[2].ToString();

                            if (VideoLink.Contains("?"))
                            {
                                VideoLink += "&vndapp=youtube_mobile";
                            }
                            else
                            {
                                VideoLink += "?vndapp=youtube_mobile";
                            }

                            return true;
                        }
                        else
                        {
                            match = Regex.Match(url, _youkuUrlTemplate1);
                            if (match.Success)
                            {
                                String sid = match.Groups[3].Value;
                                int index = sid.IndexOf("/");
                                if (index >= 0)
                                {
                                    sid = sid.Substring(0, index);
                                }

                                VideoLink = String.Format(_youku3GUrl, sid);
                                return true;
                            }
                            else
                            {
                                VideoLink = url;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public override bool MergeWith(ContentBlock contentBlock)
        {
            return false;
        }
    }

    public class QuoteBlock : TextContentBlock
    {
        private String _quoteAuthorLineTemplate1 = "【 在(?<1>.*)的大作中提到: 】";
        private String _quoteAuthorLineTemplate2 = "【 在(?<1>.*)的大作中提到:】";
        private String _quoteAuthorLineTemplate3 = "【 在(?<1>.*)的来信中提到: 】";
        private String _quoteAuthorLineTemplate4 = "【 在(?<1>.*)的大作中提到: 】";

        private uint _mergeCounter = 0;

        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            bool textLoaded = base.LoadFromHtml(rootNode);

            if (textLoaded)
            {
                if (Text.StartsWith("【"))
                {
                    Match match = Regex.Match(Text, _quoteAuthorLineTemplate1);
                    
                    if (!match.Success)
                    {
                        match = Regex.Match(Text, _quoteAuthorLineTemplate2);
                    }

                    if (!match.Success)
                    {
                        match = Regex.Match(Text, _quoteAuthorLineTemplate3);
                    }

                    if (!match.Success)
                    {
                        match = Regex.Match(Text, _quoteAuthorLineTemplate4);
                    }

                    if (match.Success)
                    {
                        _mergeCounter = 0;
                        return true;
                    }
                    
                }
                else if (Text.StartsWith(":"))
                {
                    _mergeCounter = 0;
                    return true;
                }
            }

            return false;
        }

        private int _lineBreakCount = 0;
        public override bool MergeWith(ContentBlock contentBlock)
        {
            if (NoMerge || contentBlock.NoMerge)
            {
                return false;
            }

            if (contentBlock is TextContentBlock)
            {
                String newText = (contentBlock as TextContentBlock).Text;
                bool merge = false;

                if (contentBlock is QuoteBlock)
                {
                    merge = true;
                    _lineBreakCount = 0;
                }
                else
                {

                    if (_lineBreakCount <= 1)
                    {
                        merge = true;
                    }

                    if (newText == "\n")
                    {
                        _lineBreakCount++;
                    }
                    else if (newText.Trim('\n').Trim() != "")
                    {
                        _lineBreakCount = 0;
                    }
                }

                if (merge)
                {
                    if (!App.Settings.HideFullQuote || _mergeCounter < 4)
                    {
                        Text = Text + (contentBlock as TextContentBlock).Text;
                        while (Text.EndsWith("\n\n"))
                        {
                            Text = Text.Substring(0, Text.Length - 1);
                        }

                        _mergeCounter++;
                    }

                    return true;
                }
                else
                {
                    if (newText.Trim('\n').Trim() == "")
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
