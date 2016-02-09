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
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    [XmlInclude(typeof(MitbbsPostMobile))]
    [XmlInclude(typeof(MitbbsPost))]
    [XmlInclude(typeof(MitbbsEssencePostMobile))]
    public abstract class MitbbsPostBase
    {
        protected String _authorTemplate = "(?<1>.*)\\((?<2>.*)\\)";

        public String ParentUrl { get; set; }
        public String Title { get; set; }
        public String BoardName { get; set; }
        public String Author { get; set; }
        public String AuthorId { get; set; }
        public String Source1 { get; set; }
        public String Source2 { get; set; }
        public String IssueDate { get; set; }
        public String ReplyPostUrl { get; set; }
        public String DeletePostUrl { get; set; }
        public String ModifyPostUrl { get; set; }
        public String PostUrl { get; set; }
        public String PostId { get; set; }
        public String ForwardUrl { get; set; }

        public bool IgnoreMissingHeader { get; set; }

        public ObservableCollection<ContentBlock> Contents { get; set; }
        public int SignatureStart { get; set; }

        protected int _signatureLastPos;

        public MitbbsPostBase()
        {
            Contents = new ObservableCollection<ContentBlock>();
            IgnoreMissingHeader = false;
            SignatureStart = -1;
            _signatureLastPos = -1;
        }

        public String GetText()
        {
            String text = "";

            text += "标题：" + Title + "\n";
            text += "作者：" + Author + "\n";
            text += "日期：" + IssueDate + "\n";

            for (int i = 0; i < Contents.Count; i++)
            {
                ContentBlock content = Contents[i];
                if (content is TextContentBlock)
                {
                    text += (content as TextContentBlock).Text + "\n";
                }
                else if (content is LinkBlock)
                {
                    text += (content as LinkBlock).Url + "\n";
                }
                else if (content is ImageBlock)
                {
                    text += (content as ImageBlock).ImageUrl + "\n";
                }
                else if (content is VideoBlock)
                {
                    text += "<视频内容>\n";
                }
            }

            return text;
        }

        public abstract bool LoadFromHtml(HtmlNode RootNode);

        protected void CalculateAuthorId()
        {
            if (Author != null)
            {
                Match match = Regex.Match(Author, _authorTemplate);
                if (match.Success)
                {
                    AuthorId = match.Groups[1].Value.Trim();
                }
            }
        }
    }

    public class MitbbsPostMobile : MitbbsPostBase
    {
        private String _headingTemplate = "发信人: (?<1>.*), 信区: (?<2>.*)";
        private String _headingTemplate2 = "寄信人: (?<1>.*)";
        private String _titleTemplate = "标&nbsp; 题: (?<1>.*)";
        private String _titleTemplate2 = "标&nbsp;&nbsp;题:(?<1>.*)";
        private String _source1Template = "发信站: (?<1>.*)\\((?<2>.*)\\)";
        private String _source1Template2 = "发信站: (?<1>.*) \\((?<2>.*)\\)";
        private String _source2Template = "※ 来源:・(?<1>.*)";
        
        public override bool LoadFromHtml(HtmlNode RootNode)
        {
            if(LoadPart1(RootNode.ChildNodes[1]) &&
                LoadPart2(RootNode.ChildNodes[0]))
            {
                if (!String.IsNullOrEmpty(BoardName) && !String.IsNullOrEmpty(PostId))
                {
                    ForwardUrl = String.Format(App.Settings.BuildUrl(@"/mitbbs_forward.php?board={0}&id={1}"), BoardName, PostId);
                }

                return true;
            }

            return false;
        }

        protected bool LoadPart1(HtmlNode RootNode)
        {
            String temp;
            Match match;
            HtmlNodeCollection contentNodes = RootNode.ChildNodes;

            // Heading
            //
            int startOffset = 0;
            if (contentNodes[startOffset + 0].Name == "#text")
            {
                temp = HtmlUtilities.GetPlainHtmlText(contentNodes[0].InnerText);
                temp = temp.Replace('<', '《').Replace('>', '》');
                match = Regex.Match(temp, _headingTemplate);
                if (match.Success)
                {
                    Author = match.Groups[1].ToString();
                    BoardName = match.Groups[2].ToString();
                }
                else
                {
                    match = Regex.Match(temp, _headingTemplate2);
                    if (match.Success)
                    {
                        Author = match.Groups[1].ToString();
                        BoardName = null;
                    }
                    else
                    {
                        Author = "Unknown";
                        IssueDate = "Unknown";
                    }
                }

                if (!match.Success && !IgnoreMissingHeader)
                {
                    return false;
                }
            }
            else if (!IgnoreMissingHeader)
            {
                return false;
            }

            CalculateAuthorId();

            // Title
            //
            Title = "无标题";
            if (contentNodes[startOffset + 2].Name == "#text")
            {
                temp = contentNodes[startOffset + 2].InnerText.Trim('\n');
                match = Regex.Match(temp, _titleTemplate);
                if (!match.Success)
                {
                    match = Regex.Match(temp, _titleTemplate2);
                }

                if (match.Success)
                {
                    Title = HtmlUtilities.GetPlainHtmlText(match.Groups[1].ToString());
                }
                else if (!IgnoreMissingHeader)
                {
                    return false;
                }
            }
            else if (!IgnoreMissingHeader)
            {
                return false;
            }
            
            // Source 1
            //
            int source1Index = startOffset + 4;
            if (contentNodes[source1Index].Name == "#text")
            {
                temp = HtmlUtilities.GetPlainHtmlText(contentNodes[source1Index].InnerText);
                match = Regex.Match(temp, _source1Template);

                if (!match.Success)
                {
                    match = Regex.Match(temp, _source1Template2);
                }

                if (!match.Success)
                {
                    source1Index += 2;
                    temp = HtmlUtilities.GetPlainHtmlText(contentNodes[source1Index].InnerText);
                    match = Regex.Match(temp, _source1Template);
                }

                if (match.Success)
                {
                    Source1 = match.Groups[1].ToString();
                    IssueDate = match.Groups[2].ToString();
                }
                else if (!IgnoreMissingHeader)
                {
                    return false;
                }
                else
                {
                    source1Index = -3;
                }
            }
            else if (!IgnoreMissingHeader)
            {
                return false;
            }
            else
            {
                source1Index = startOffset - 3;
            }

            // Generate all content block
            //
            bool hasQuote = false;
            ContentBlock prevContentBlock = null;
            for (int i = (source1Index + 3); i < contentNodes.Count; i++)
            {
                HtmlNode contentNode = contentNodes[i];
                String text = "";

                if (contentNode.Name == "#text")
                {
                    text = HtmlUtilities.GetPlainHtmlText(contentNode.InnerText);

                    if (text.StartsWith("※"))
                    {
                        match = Regex.Match(text, _source2Template);
                        if (match.Success)
                        {
                            Source2 = match.Groups[1].ToString();
                        }

                        continue;
                    }
                }
                else if ((contentNode.Name == "font") && (contentNode.ChildNodes.Count > 0))
                {
                    contentNode = contentNode.ChildNodes[0];
                }

                ContentBlock contentBlock = ContentBlock.CreateContentBlockFromHtml(contentNode, ParentUrl);
                
                if (contentBlock != null)
                {
                    bool isSignatureStart = false;
                    if(text == "--")
                    {
                        hasQuote = false;
                        isSignatureStart = true;
                        contentBlock.NoMerge = true;
                    }

                    if (SignatureStart >= 0 && contentBlock is TextContentBlock && (contentBlock as TextContentBlock).Text == "\n")
                    {
                        contentBlock.NoMerge = true;
                    }

                    if ((prevContentBlock == null) || !(prevContentBlock.MergeWith(contentBlock)))
                    {
                        if (!hasQuote  || !(contentBlock is QuoteBlock) || !App.Settings.HideFullQuote)
                        {
                            Contents.Add(contentBlock);
                            prevContentBlock = contentBlock;

                            if (prevContentBlock != null)
                            {
                                if (prevContentBlock is QuoteBlock)
                                {
                                    hasQuote = true;
                                }
                            }

                            if (isSignatureStart)
                            {
                                _signatureLastPos = SignatureStart;
                                SignatureStart = Contents.Count - 1;
                            }
                        }
                    }
                }
            }

            if ((prevContentBlock != null) && (prevContentBlock is TextContentBlock))
            {
                (prevContentBlock as TextContentBlock).Text = (prevContentBlock as TextContentBlock).Text.Trim('\n');
            }

            if (_signatureLastPos >= 0)
            {
                SignatureStart = _signatureLastPos;
            }

            return true;
        }

        protected bool LoadPart2(HtmlNode RootNode)
        {
            IEnumerable<HtmlNode> linkNodes = RootNode.Descendants("a");
            foreach (HtmlNode linkNode in linkNodes)
            {
                String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText);
                if (linkText == "回复")
                {
                    ReplyPostUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                }
                else if (linkText == "删除")
                {
                    DeletePostUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                }
                else if (linkText == "修改")
                {
                    ModifyPostUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                }
                else if (linkText == "全文")
                {
                    PostUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);

                    String site;
                    Dictionary<String, String> values;
                    HtmlUtilities.ParseQueryStringFromUrl(PostUrl, out site, out values);
                    if (values.ContainsKey("id"))
                    {
                        PostId = values.GetValue("id");
                    }
                }
            }

            return true;
        }
    }

    public class MitbbsEssencePostMobile : MitbbsPostMobile
    {
        public override bool LoadFromHtml(HtmlNode RootNode)
        {
            return LoadPart1(RootNode.ChildNodes[1]);
        }
    }

    public class MitbbsMailPostMobile : MitbbsPostMobile
    {
        public bool LoadMailBodyFromHtml(HtmlNode RootNode)
        {
            return LoadPart1(RootNode);
        }

        public bool LoadMailFooterFromHtml(HtmlNode RootNode)
        {
            IEnumerable<HtmlNode> linkNodes = RootNode.Descendants("a");
            foreach (HtmlNode linkNode in linkNodes)
            {
                String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText);
                if (linkText == "回信")
                {
                    ReplyPostUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                }
                else if (linkText == "删除")
                {
                    DeletePostUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                }
            }

            return true;
        }
    }

    public class MitbbsPost : MitbbsPostBase
    {
        private String _headingTemplate = "发信人: (?<1>.*), 信区: (?<2>.*)";
        private String _titleTemplate = "标  题: (?<1>.*)";
        private String _source1Template = "发信站: (?<1>.*)\\((?<2>.*)\\)";
        private String _source2Template = "※ 来源:·(?<1>.*)";

        public override bool LoadFromHtml(HtmlNode RootNode)
        {
            try
            {
                uint textIndex = 0;
                bool hasQuote = false;
                ContentBlock prevContentBlock = null;
                bool startContent = false;

                foreach (HtmlNode node in RootNode.Descendants())
                {
                    ContentBlock contentBlock = null;
                    String text = "";

                    if (node.Name == "#text")
                    {
                        text = node.GetPlainInnerText();
                        Match match;
                        switch (textIndex)
                        {
                            case 1:
                                match = Regex.Match(text, _headingTemplate);
                                if (match.Success)
                                {
                                    Author = match.Groups[1].ToString();
                                    BoardName = match.Groups[2].ToString();

                                    CalculateAuthorId();
                                }
                                break;

                            case 2:
                                match = Regex.Match(text, _titleTemplate);
                                if (match.Success)
                                {
                                    Title = HtmlUtilities.GetPlainHtmlText(match.Groups[1].ToString());
                                }
                                break;

                            case 3:
                            case 4:
                                match = Regex.Match(text, _source1Template);
                                if (match.Success)
                                {
                                    Source1 = match.Groups[1].ToString();
                                    IssueDate = match.Groups[2].ToString();
                                }

                                if (!text.StartsWith("关键字:"))
                                {
                                    if ((Author == null) ||
                                        (BoardName == null) ||
                                        (Title == null) ||
                                        (Source1 == null) ||
                                        (IssueDate == null))
                                    {
                                        return false;
                                    }
                                }

                                break;

                            default:
                                if (text.StartsWith("※"))
                                 {
                                    match = Regex.Match(text, _source2Template);
                                    if (match.Success)
                                    {
                                        Source2 = match.Groups[1].ToString();
                                        break;
                                    }
                                }

                                if (node.ParentNode.Name == "a")
                                {
                                    break;
                                }
                                
                                if (startContent && !String.IsNullOrEmpty(text))
                                {
                                    contentBlock = ContentBlock.CreateContentBlockFromHtml(node, ParentUrl);
                                }

                                break;
                        }

                        if (textIndex > 3)
                        {
                            startContent = true;
                        }

                        textIndex++;
                    }
                    else if (startContent)
                    {
                        if (node.Name == "a")
                        {
                            String link = node.Attributes["href"].Value;
                            if (link.StartsWith(@"javascript:"))
                            {
                                continue;
                            }
                        }
                        else if (node.Name == "img")
                        {
                            if (node.ParentNode.Name == "a")
                            {
                                continue;
                            }
                        }

                        contentBlock = ContentBlock.CreateContentBlockFromHtml(node, ParentUrl);
                    }

                    if (contentBlock != null)
                    {
                        bool isSignatureStart = false;
                        if (text == "--")
                        {
                            hasQuote = false;
                            isSignatureStart = true;
                            contentBlock.NoMerge = true;
                        }

                        if (prevContentBlock != null && SignatureStart >= 0 && contentBlock is TextContentBlock && (contentBlock as TextContentBlock).Text == "\n")
                        {
                            prevContentBlock.NoMerge = true;
                        }

                        if ((prevContentBlock == null) || !(prevContentBlock.MergeWith(contentBlock)))
                        {
                            if (!hasQuote || !(contentBlock is QuoteBlock) || !App.Settings.HideFullQuote)
                            {
                                Contents.Add(contentBlock);
                                prevContentBlock = contentBlock;

                                if (prevContentBlock != null)
                                {
                                    if (prevContentBlock is QuoteBlock)
                                    {
                                        hasQuote = true;
                                    }
                                }

                                if (isSignatureStart)
                                {
                                    _signatureLastPos = SignatureStart;
                                    SignatureStart = Contents.Count - 1;
                                }
                            }
                        }
                    }
                }

                if (_signatureLastPos >= 0)
                {
                    SignatureStart = _signatureLastPos;
                }

                HtmlNode pTable = RootNode.GetParentOfType("table");
                HtmlNode prevTable = pTable.GetPrevSiblingOfType("table");
                if (prevTable != null)
                {
                    foreach (HtmlNode linkNode in prevTable.Descendants("a"))
                    {
                        String linkText = linkNode.GetLinkText();
                        String linkUrl = linkNode.GetLinkUrl(ParentUrl);

                        if (linkText == "本篇全文")
                        {
                            PostUrl = linkUrl;
                        }
                        else if (linkText == "回复")
                        {
                            ReplyPostUrl = linkUrl;
                        }
                        else if (linkText == "修改")
                        {
                            ModifyPostUrl = linkUrl;
                        }
                        else if (linkText == "删除")
                        {
                            if (linkNode.Attributes.Contains("onclick"))
                            {
                                DeletePostUrl = linkNode.Attributes["onclick"].Value;
                            }
                        }
                        else if (linkText == "转贴")
                        {
                            ForwardUrl = linkUrl;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
