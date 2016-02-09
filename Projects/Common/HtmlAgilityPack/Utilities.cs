using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace HtmlAgilityPack
{
    public class DataLoadedEventArgs : EventArgs
    {
        public Exception Error;
    }

    public static class HtmlUtilities
    {
        public static TValue GetDictionaryValueOrNull<TKey,TValue>(Dictionary<TKey,TValue> dict, TKey key) where TKey: class
        {
            return dict.ContainsKey(key) ? dict[key] : default(TValue);
        }

        public static String GetPlainHtmlText(String htmlText)
        {
            char [] trimChars= new char[2];
            trimChars[0] = ' ';
            trimChars[1] = '\n';

            return HttpUtility.HtmlDecode(htmlText).Trim(trimChars).Trim();
        }

        public static String GetAbsoluteUrl(String baseUrl, String url)
        {
            Uri uri = new Uri(new Uri(baseUrl, UriKind.Absolute), url);
            return uri.AbsoluteUri;
        }

        public static String GetPlainInnerText(this HtmlNode node)
        {
            if (node == null)
            {
                return null;
            }

            return HtmlUtilities.GetPlainHtmlText(node.InnerText);
        }

        public static String GetLinkUrl(this HtmlNode node, String baseUrl = null)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Attributes.Contains("href"))
            {
                String url = ExtractJSLinkUrl(node.Attributes["href"].Value);

                if (baseUrl == null)
                {
                    return url;
                }
                else
                {
                    return GetAbsoluteUrl(baseUrl, url);
                }
            }

            return null;
        }

        private static String ExtractJSLinkUrl(String link)
        {
            const String jsUrlTemplate1 = @"javascript:myhref\((?<1>.*)\)";
            const String jsUrlTemplate2 = @"javascript:botpagehref\((?<1>.*)\)";

            Match match = Regex.Match(link, jsUrlTemplate1);
            if (!match.Success)
            {
                match = Regex.Match(link, jsUrlTemplate2);
            }

            if (match.Success)
            {
                String newLink = match.Groups[1].Value;
                newLink = newLink.Trim('"');
                newLink = newLink.Trim('\'');

                return newLink;
            }
            else
            {
                return link;
            }
        }

        public static String GetLinkText(this HtmlNode node)
        {
            return node.GetPlainInnerText();
        }

        public static HtmlNode GetParentOfType(this HtmlNode node, string type)
        {
            if (node == null)
            {
                return null;
            }

            HtmlNode result = node.ParentNode;
            while ((result != null) && (result.Name != type))
            {
                result = result.ParentNode;
            }

            return result;
        }

        public static HtmlNode GetPrevSiblingOfType(this HtmlNode node, string type)
        {
            if (node == null)
            {
                return null;
            }

            HtmlNode result = node.PreviousSibling;
            while ((result != null) && (result.Name != type))
            {
                result = result.PreviousSibling;
            }

            return result;
        }

        public static HtmlNode GetNextSiblingOfType(this HtmlNode node, string type)
        {
            if (node == null)
            {
                return null;
            }

            HtmlNode result = node.NextSibling;
            while ((result != null) && (result.Name != type))
            {
                result = result.NextSibling;
            }

            return result;
        }

        public static void ParseQueryStringFromUrl(String url, out String site, out Dictionary<String, String> values)
        {
            values = new Dictionary<string,string>();

            int index = url.IndexOf('?');
            if (index >= 0 && index < url.Length - 1)
            {
                site = url.Substring(0, index);
                String queryString = url.Substring(index + 1, url.Length - index - 1);
                
                String[] valuePairs = queryString.Split('&');
                foreach (String valuePair in valuePairs)
                {
                    String name;
                    String value;
                    index = valuePair.IndexOf('=');
                    if (index >= 0)
                    {
                        name = valuePair.Substring(0, index);
                        value = Uri.UnescapeDataString(valuePair.Substring(index + 1, valuePair.Length - index - 1));
                    }
                    else
                    {
                        name = valuePair;
                        value = "";
                    }

                    values.Add(name, value);
                }
            }
            else
            {
                site = url;
            }
        }

        public static String ConstructUrl(String site, Dictionary<String, String> values)
        {
            String url = site;

            if (values.Count > 0)
            {
                url += "?";
            }

            foreach (String name in values.Keys)
            {
                String value;
                values.TryGetValue(name, out value);
                url += name + "=" + Uri.EscapeDataString(value);
            }

            return url;
        }

        public static T2 GetValue<T1, T2>(this Dictionary<T1, T2> dict, T1 key)
        {
            T2 value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return default(T2);
            }
        }

        public static String EncodeUrlIntoFilename(String url)
        {
            StringBuilder sb = new StringBuilder(url.Length);
            foreach (char c in url)
            {
                char c2 = c;
                if (c2 == '/' || c2 == ':' || c2 == '?' || c2== '=' || c2== '&')
                {
                    c2 = '_';
                }

                sb.Append(c2);
            }

            return sb.ToString();
        }

        public static bool TryParseGuid(String guidStr, out Guid guid)
        {
#if NODO
            try
            {
                guid = new Guid(guidStr);
                return true;
            }
            catch
            {
                guid = new Guid();
                return false;
            }

#else
            return Guid.TryParse(guidStr, out guid);
#endif
        }
    }

}
