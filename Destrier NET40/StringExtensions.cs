using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Net.Mail;
using System.Globalization;

namespace Destrier.Extensions
{
    public static class StringExtensions
    {
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

        public static string ToTitleCase(this String mText)
        {
            System.Globalization.CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Globalization.TextInfo TextInfo = cultureInfo.TextInfo;
            return TextInfo.ToTitleCase(mText);
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
    }
}
