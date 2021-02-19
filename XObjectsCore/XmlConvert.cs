//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Xml.Schema.Linq
{
    internal class XmlConvertExt
    {
        static char[] crt = new char[] {'\n', '\r', '\t'};

        public static Exception VerifyNormalizedString(string str)
        {
            if (str.IndexOfAny(crt) != -1)
            {
                return new LinqToXsdException("Failed to Verify Normalized String: " + str);
            }

            return null;
        }

        static readonly char[] WhitespaceChars = new char[] {' ', '\t', '\n', '\r'};

        public static Exception TryToUri(string s, out Uri result)
        {
            result = null;

            if (s != null && s.Length > 0)
            {
                //string.Empty is a valid uri but not "   "
                s = TrimString(s);
                ;
                if (s.Length == 0 || s.IndexOf("##", StringComparison.Ordinal) != -1)
                {
                    return new FormatException();
                }
            }

            if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out result))
            {
                return new FormatException();
            }

            return null;
        }

        public static Uri ToUri(string s)
        {
            if (s != null && s.Length > 0)
            {
                //string.Empty is a valid uri but not "   "
                s = TrimString(s);
                if (s.Length == 0 || s.IndexOf("##", StringComparison.Ordinal) != -1)
                {
                    throw new FormatException();
                }
            }

            Uri uri;
            if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out uri))
            {
                throw new FormatException();
            }

            return uri;
        }


        // Trim a string using XML whitespace characters
        public static string TrimString(string value)
        {
            return value.Trim(WhitespaceChars);
        }

        // Split a string into a whitespace-separated list of tokens
        public static string[] SplitString(string value)
        {
            return value.Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}