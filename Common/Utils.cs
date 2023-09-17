
using Common.Properties;
using Common.Serializer;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common
{
    public class Utils
    {
        public static string GetWellformedUrlString(string baseUrl, string url)
        {
            if (url == null) return baseUrl;

            if (Uri.IsWellFormedUriString(url, UriKind.Relative))
            {
                Uri uriOut = null;
                var options = new UriCreationOptions();
                options.DangerousDisablePathAndQueryCanonicalization = true;

                Uri.TryCreate(string.Concat(baseUrl, url), options, out uriOut);

                return uriOut.AbsoluteUri;
            }

            return url;
        }

        public static string GetSafeSuffix(string localPath)
        {
            //Create suffix for output file
            Regex r = new Regex("(?:[^a-z0-9]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return r.Replace(localPath, String.Empty);

        }

        internal static string? GetUrlDomain(string url)
        {
            Uri uriOut = null;
            var options = new UriCreationOptions();
            options.DangerousDisablePathAndQueryCanonicalization = true;

            if(Uri.TryCreate(url, options, out uriOut))
                return uriOut.Host;
            else return null;
        }

        internal static bool ContainsText(object valueA, string valueB)
        {
            Regex regex = new Regex("[^a-zA-Z0-9]");
            string compA = regex.Replace((string)valueA, "");
            string compB = regex.Replace(valueB, "");
            return compA.Contains(compB);
        }
    }
    public static class HtmlAttrExtensions
    {
        public static string Stringify(this HtmlAttr htmlAttr)
        {
            //Special characters managed
            if (htmlAttr.ToString().StartsWith(Resources.SpecialHtmlAttrPrefix))
                switch (htmlAttr)
                {
                    case HtmlAttr._SharpText:
                        return "#text";
                    default:
                        return string.Empty;
                }

            return htmlAttr.ToString().ToLower();
        }

        public static HtmlAttr ToHtmlAttr(string attr)
        {
            HtmlAttr htmlAttr = new HtmlAttr();
            try
            {
                Enum.TryParse(attr.ToLower(), out htmlAttr);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return htmlAttr;
        }
    }

    public class RegexString
    {
        readonly string _value;
        public RegexString(string value)
        {
            this._value = value;
        }
        public static implicit operator string(RegexString d)
        {
            if (d == null) return null;
            return d._value;
        }
        public static implicit operator RegexString(string d)
        {
            return new RegexString(d);
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public override bool Equals(object? obj)
        {
            if (_value.Contains(Regexing.andConcat))
            {
                string[] andValues = _value.Split(Regexing.andConcat);

            }
            if (_value.Contains(Regexing.orConcat))
            {
                string[] orValues = _value.Split(Regexing.orConcat);
                foreach (string val in orValues)
                    if (new RegexString(val).Equals(obj))
                        return true;

                return false;
            }
            if (_value.Contains(Regexing.sqlModulo))
            {
                if (_value.StartsWith(Regexing.sqlModulo))
                {
                    string compareValue = _value.Replace(Regexing.sqlModulo, "");
                    return ((string)obj).EndsWith(compareValue);
                }
                if (_value.EndsWith(Regexing.sqlModulo))
                {
                    string compareValue = _value.Replace(Regexing.sqlModulo, "");
                    return ((string)obj).StartsWith(compareValue);
                }
            }

            return _value.Equals(obj);
        }

        public class EqualityComparer : IEqualityComparer<RegexString>
        {
            public bool Equals(RegexString x, RegexString y)
            {
                return x._value == y._value;
            }

            public int GetHashCode(RegexString x)
            {
                return x._value.GetHashCode();
            }
        }
    }
    public class Regexing
    {
        #region STATIC VERSION -> FOR INLINE REGEXs
        internal static readonly string child = ".";
        internal static readonly string sqlModulo = "%";
        internal static readonly string andConcat = "&";
        internal static readonly string orConcat = "|";

        public static bool HasInlineRegex(string value)
        {
            return value.Contains(child) || value.Contains(sqlModulo);
        }

        public static string ApplyInlineRegex(string value)
        {
            if (value.Contains(child))
            {

            }
            if (value.Contains(sqlModulo))
            {
                //Apply StartWith instead of Equals 

            }

            return value;
        }
        #endregion STATIC VERSION

        #region INSTANCE VERSION -> FOR DECLARED REGEXs
        public Action? action { get; set; }
        public string regex { get; set; }

        /// <summary>
        /// Generic method - beware return type expected
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyRegex<T>(string value)
        {
            switch (this.action)
            {
                case Action.REPLACE:
                    return (T)(object)value.Replace(value, regex);
                case Action.REMOVE:
                    return (T)(object)value.Replace(regex, "");
                case Action.REMOVEINSENSITIVE:
                    return (T)(object)value.Replace(regex, "", StringComparison.InvariantCultureIgnoreCase);
                case Action.SPLITONCE:
                    return (T)(object)value.Split(" ");
                default:
                    return (T)(object)value;
            }
        }

        #endregion INSTANCE VERSION
    }
}