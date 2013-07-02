using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Net.Mail;
using System.Globalization;

namespace Destrier
{
    public static class StringExtensions
    {
        #region "Constants"
        public static Char[] NTFSIllegals = Path.GetInvalidPathChars();

        public static System.Text.Encoding Encoding
        {
            get
            {
                return System.Text.UnicodeEncoding.UTF8;
            }
        }

        public static System.Text.Encoding GetDefaultEncoding(this String str)
        {
            return StringExtensions.Encoding;
        }
        #endregion

        #region Validation
        public static Boolean IsValidEmailAddress(this String str)
        {
            try
            {
                if (str.Contains(','))
                {
                    foreach (var recipient in str.Split(','))
                    {
                        var a = new MailAddress(recipient);
                    }
                }
                else
                {
                    var a = new MailAddress(str);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Conversion
        public static Boolean? ToBoolean(this String str)
        {
            if (String.IsNullOrEmpty(str))
                return null;

            switch (str.Trim().ToLower())
            {
                case "true":
                case "1":
                    return true;
                case "0":
                case "false":
                    return false;
            }

            return null;
        }

        public static Int16? ToInt16(this String str)
        {
            Int16 parseValue = default(Int16);
            Boolean parseResult = Int16.TryParse(str, out parseValue);

            if (parseResult)
                return parseValue;
            else
                return null;
        }

        public static Int32? ToInt32(this String str)
        {
            Int32 parseValue = default(Int32);
            Boolean parseResult = Int32.TryParse(str, out parseValue);

            if (parseResult)
                return parseValue;
            else
                return null;
        }

        public static Int64? ToInt64(this String str)
        {
            Int64 parseValue = default(Int64);
            Boolean parseResult = Int64.TryParse(str, out parseValue);

            if (parseResult)
                return parseValue;
            else
                return null;
        }

        public static T ToEnum<T>(this String str)
        {
            if(str.IsNumeric())
            {
                var enumValues = EnumUtil.Explode<T>();
                var enumValue = enumValues.FirstOrDefault(ev => ev.Id == str.ToInt32());

                if (enumValue != null)
                    return enumValue.Value;
            }
            else //string search
            {
                var enumValues = EnumUtil.Explode<T>();
                var enumValue = enumValues.FirstOrDefault(ev => ev.Name.Equals(str, StringComparison.InvariantCultureIgnoreCase));

                if (enumValue != null)
                    return enumValue.Value;
            }

            return default(T);
        }

        public static Single? ToFloat(this String str)
        {
            Single parseValue = default(Single);
            Boolean parseResult = Single.TryParse(str, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out parseValue);

            if (parseResult)
                return parseValue;
            else
                return null;
        }

        public static Double? ToDouble(this String str)
        {
            //two possibilities, US and EU
            //remove non-numeric characters except for the last one. 
                //if this character is a ,

            //123,00
            //123,000 <-- valid both in eu format and us format, wildly different values


            Double parseValue = default(Double);
            Boolean parseResult = Double.TryParse(str, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out parseValue);

            if (parseResult)
                return parseValue;
            else
                return null;
        }

        public static DateTime? ToDateTime(this String str, Boolean isBinary = false)
        {
            DateTime parseValue = default(DateTime);
            Boolean parseResult = false;
            if (isBinary)
            {
                try
                {
                    parseValue = DateTime.FromBinary((Int64)str.ToInt64());
                    parseResult = true;
                }
                catch
                {
                    parseResult = false;
                }
            }
            else
	        {
                parseResult = DateTime.TryParse(str, out parseValue);
	        }

            if (parseResult)
                return parseValue;
            else
                return null;
        }

        public static Guid? ToGuid(this String str)
        {
            Guid parseValue = default(Guid);
            Boolean parseResult = Guid.TryParse(str, out parseValue);

            if (parseResult)
                return parseValue;
            else
                return null;
        }
        #endregion

        public static Byte[] GetBytes(this String str)
        {
            if (String.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            return Encoding.GetBytes(str);
        }

        public static String AsCleanFilename(this String fileName)
        {
            if ((null != fileName) && (String.Empty != fileName))
            {
                if (fileName.Length > 255)
                {
                    fileName = fileName.Substring(0, 254);
                }
                String sOut = fileName.Trim();
                sOut = ReplaceAny(sOut, @"", new String[] { @"&gt;", @"&lt;" });
                return ReplaceAny(sOut, @"", NTFSIllegals.Cast<String>());
            }
            else
            {
                return String.Empty;
            }
        }

        public static String GetFilenameRoot(this String inFilename)
        {
            if (!inFilename.Contains('.'))
            {
                return inFilename;
            }
            return inFilename.Substring(0, inFilename.LastIndexOf('.'));
        }

        public static String GetFilenameExt(this String inFilename)
        {
            return Path.GetExtension(inFilename);
        }

        public static String GetFirstWithin(this String input, Char enclosing)
        {
            int begin = 0;
            int end = 0;
            int state = 0;
            String toReturn = String.Empty;
            for (int x = 0; x < input.Length; x++)
            {
                switch (state)
                {
                    case 0:
                        if (input[x] == enclosing)
                        {
                            begin = x;
                            state = 1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    case 1:
                        if (input[x] == enclosing)
                        {
                            state = 2;
                            end = x;
                        }
                        else
                        {
                            toReturn = toReturn + input[x];
                        }
                        break;
                    case 2:
                        break;
                    default:
                        break;
                }
            }
            return toReturn;
        }

        public static String RemoveHTMLTags(this String inText)
        {
            String dec = System.Web.HttpUtility.HtmlDecode(inText);
            char[] txt = inText.ToCharArray();
            StringBuilder sb = new StringBuilder();
            int state = 0;

            for (int x = 0; x < txt.Length; x++)
            {
                char c = txt[x];
                switch (state)
                {
                    case 0:
                        //this is the default state
                        //we are not within a tag and are looking for a new start tag.
                        if (c == '<')
                        {
                            state = 1;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                    case 1:
                        //we are within a tag, look for the end >
                        if (c == '>')
                        {
                            state = 0;
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        public static String ReplaceAny(this String toProcess, String replaceWith, params String[] toReplace)
        {
            foreach (String s in toReplace)
            {
                toProcess = toProcess.Replace(s, replaceWith);
            }
            return toProcess;
        }

        public static String ReplaceAny(this String toProcess, String replaceWith, IEnumerable<String> toReplace)
        {
            foreach (String s in toReplace)
            {
                toProcess = toProcess.Replace(s, replaceWith);
            }
            return toProcess;
        }

        public static string ToTitleCase(this String mText)
        {
            string rText = "";
            try
            {
                System.Globalization.CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Globalization.TextInfo TextInfo = cultureInfo.TextInfo;
                rText = TextInfo.ToTitleCase(mText);
            }
            catch
            {
                rText = mText;
            }
            return rText;
        }

        public static string ToLowerCaseFirstLetter(this String mText)
        {
            if (mText.Length == 0)
                return mText;

            Char[] chars = mText.ToCharArray();

            chars[0] = Char.ToLowerInvariant(chars[0]);
            return new String(chars);
        }

        public static String ToNumericsOnly(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            if (text.Length < 100)
            {
                String output = String.Empty;
                for (int x = 0; x < text.Length; x++)
                {
                    char c = text[x];
                    if (Char.IsNumber(c))
                        output += c;
                }

                return output;
            }
            else
            {
                StringBuilder output = new StringBuilder();
                for (int x = 0; x < text.Length; x++)
                {
                    char c = text[x];
                    if (Char.IsNumber(c))
                        output.Append(c);
                }

                return output.ToString();
            }
        }

        public static String ToNonNumericsOnly(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            if (text.Length < 100)
            {
                String output = String.Empty;
                for (int x = 0; x < text.Length; x++)
                {
                    char c = text[x];
                    if (!Char.IsNumber(c))
                        output += c;
                }

                return output;
            }
            else
            {
                StringBuilder output = new StringBuilder();
                for (int x = 0; x < text.Length; x++)
                {
                    char c = text[x];
                    if (!Char.IsNumber(c))
                        output.Append(c);
                }

                return output.ToString();
            }
        }

        public static Double? ParseMoneyInternational(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return null;

            var textBuilder = new StringBuilder(text);

            var non_numerics = text.ToNonNumericsOnly();
            if (non_numerics.Any())
            {
                var last_non_numeric = non_numerics.Last();

                if (last_non_numeric == ',')
                {
                    var position = text.LastIndexOf(last_non_numeric);

                    if (position > 2)
                    {
                        textBuilder.Replace(".", "#");
                        textBuilder[position] = '.';
                        textBuilder.Replace(",", "");
                        textBuilder.Replace("#", "");
                    }
                }
            }

            double value = default(double);
            var stringValue = textBuilder.ToString().Trim();
            var numberFormat = (System.Globalization.NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            numberFormat.CurrencyDecimalSeparator = ".";
            numberFormat.NumberDecimalSeparator = ".";
            numberFormat.PercentDecimalSeparator = ".";

            var success = double.TryParse(stringValue, NumberStyles.Any, numberFormat, out value);
            return success ? (double?)value : null;
        }

        public static Boolean IsNumeric(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return false;

            Double testVar = default(Double);
            return Double.TryParse(text, out testVar);
        }

        public static String HtmlDecode(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            return System.Web.HttpUtility.HtmlDecode(text);
        }

        public static String HtmlEncode(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            return System.Web.HttpUtility.HtmlEncode(text);
        }

        public static String UrlEncode(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            return System.Web.HttpUtility.UrlEncode(text);
        }

        public static String UrlDecode(this String text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            return System.Web.HttpUtility.UrlDecode(text);
        }

        public static String Encode64bit(this String plainText)
        {
            return Convert.ToBase64String(new System.Text.UnicodeEncoding().GetBytes(plainText));
        }

        public static String Decode64bit(this String cypherText)
        {
            return new String(new System.Text.UnicodeEncoding().GetChars(Convert.FromBase64String(cypherText)));
        }

        public static string Inject(this String text, String searchTerm, String format)
        {
            if (String.IsNullOrEmpty(searchTerm))
                return text;

            if (String.IsNullOrEmpty(format))
                return text;

            var sb = new StringBuilder();
            var length = text.Length;

            var state = 0;
            var matches = new List<Tuple<Int32, Int32>>();
            var mutableSearchTerm = new String(searchTerm.ToArray());

            var startIndex = 0;
            var endIndex = 0;
            for (int index = 0; index < length; index++)
            {
                var charAtIndex = Char.ToLower(text[index]);

                switch (state)
                {
                    case 0:
                        if (Char.ToLower(mutableSearchTerm[0]) == charAtIndex)
                        {
                            startIndex = index;
                            mutableSearchTerm = mutableSearchTerm.Substring(1, mutableSearchTerm.Length - 1);
                            state = 1;
                        }
                        break;
                    case 1:
                        if (String.IsNullOrEmpty(mutableSearchTerm))
                        {
                            endIndex = index;
                            matches.Add(Tuple.Create<Int32, Int32>(startIndex, endIndex));
                            startIndex = 0;
                            endIndex = 0;
                            state = 0;
                            mutableSearchTerm = new String(searchTerm.ToArray());
                        }
                        else if (Char.ToLower(mutableSearchTerm[0]) == charAtIndex)
                        {
                            mutableSearchTerm = mutableSearchTerm.Substring(1, mutableSearchTerm.Length - 1);

                            if ((index == length - 1) && String.IsNullOrEmpty(mutableSearchTerm)) // if this is the last character and we've just finished off a match ...
                            {
                                endIndex = index + 1;
                                matches.Add(Tuple.Create<Int32, Int32>(startIndex, endIndex));
                            }
                        }
                        else
                        {
                            startIndex = 0;
                            endIndex = 0;
                            state = 0;
                            mutableSearchTerm = new String(searchTerm.ToArray());
                        }
                        break;
                }
            }

            if (matches.Any())
            {
                var index = 0;
                var lastSplit = 0;
                foreach (var match in matches)
                {
                    var toReplace = text.Substring(match.Item1, match.Item2 - match.Item1);
                    var replaceWith = String.Format(format, toReplace);

                    var before = text.Substring(lastSplit, match.Item1 - lastSplit);
                    var after = text.Substring(match.Item2, text.Length - match.Item2);

                    sb.Append(before);
                    sb.Append(replaceWith);

                    index++;
                    if (index == matches.Count)
                    {
                        sb.Append(after);
                    }

                    if (index > 0)
                        lastSplit = match.Item2;
                }
            }
            else
            {
                sb = new StringBuilder(text);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns if any of the string appear in the block of text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="theseStrings"></param>
        /// <returns></returns>
        public static Boolean HasIn(this String text, params String[] theseStrings)
        {
            return theseStrings.Any(str => str.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Reduces whitepace blocks (either \t or '  ') to single spaces (or whatever you set whiteSpaceChar to.)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="removeNewLines"></param>
        /// <param name="replaceNewLines"></param>
        /// <param name="whiteSpaceChar"></param>
        /// <returns></returns>
        public static String CompressWhitespace(this String text, Boolean removeNewLines = true, Char? whiteSpaceChar = ' ')
        {
            String inputText = text.Trim();
            StringBuilder output = new StringBuilder();
            Int32 state = 0;
            for (int x = 0; x < inputText.Length; x++)
            {
                char c = inputText[x];
                switch (state)
                {
                    case 0:
                        if (Char.IsWhiteSpace(c))
                        {
                            if (c == '\n')
                            {
                                if (!removeNewLines)
                                {
                                    output.Append('\n');
                                }
                                else
                                {
                                    if (whiteSpaceChar != null)
                                    {
                                        output.Append(whiteSpaceChar);
                                    }
                                }
                            }
                            else
                            {
                                if (whiteSpaceChar != null)
                                {
                                    output.Append(whiteSpaceChar);
                                }
                            }

                            state = 1;
                        }
                        else
                        {
                            output.Append(c);
                        }
                        break;
                    case 1:
                        if (!Char.IsWhiteSpace(c))
                        {
                            output.Append(c);
                            state = 0;
                        }
                        break;
                }
            }

            return output.ToString();
        }
    }
}
