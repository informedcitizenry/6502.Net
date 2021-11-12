//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Core6502DotNet.Json
{
    /// <summary>
    /// Defines an interface to validate that a string value conforms to 
    /// a given format.
    /// </summary>
    public interface IFormatValidator
    {
        /// <summary>
        /// Determines if a value conforms to a given format.
        /// </summary>
        /// <param name="formatName">The format name.</param>
        /// <param name="value">The string value.</param>
        /// <returns><c>true</c> if the string value conforms to the format definition,
        /// <c>false</c> otherwise.</returns>
        bool FormatIsValid(string formatName, string value);
    }

    /// <summary>
    /// A class that validates a string representing a date/time or duration value is of a 
    /// given format.
    /// </summary>
    public class DateTimeValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
        {
            var valid = formatName switch
            {
                "date"      => Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}$"),
                "time"      => Regex.IsMatch(value, @"^\d{2}:\d{2}:\d{2}$"),
                "duration"  => Regex.IsMatch(value, @"^P(((\d+Y)?(\d+M)?(\d+W)?(\d+D)?)?(T(\d+H)?(\d+M)?(\d+S)?)?)$"),
                _           => Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$")
            };
            if (valid)
            {
                if (formatName.Equals("duration"))
                    return !value.Equals("P");
                return DateTime.TryParse(value, out _);
            }
            return false;
        }
    }

    /// <summary>
    /// A class that validates whether a string represents a valid email address.
    /// </summary>
    public class EmailValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
        {
            try
            {
                var email = Regex.Replace(value, @"(@)(.+)$", DomainMapper, RegexOptions.None,
                    TimeSpan.FromMilliseconds(200));
                return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(250));

                static string DomainMapper(Match match)
                {
                    var idn = new IdnMapping();
                    string domainName = idn.GetAscii(match.Groups[2].Value);
                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// A class that validates whether a string value represents a valid URI.
    /// </summary>
    public class UriValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
        {
            if (formatName.EndsWith("-template", StringComparison.Ordinal))
                return Regex.IsMatch(value, @"^([^\x00-\x20\x7f""'%<>\\^`{|}]|%[0-9A-Fa-f]{2}|{[+#./;?&=,!@|]?((\w|%[0-9A-Fa-f]{2})(\.?(\w|%[0-9A-Fa-f]{2}))*(:[1-9]\d{0,3}|\*)?)(,((\w|%[0-9A-Fa-f]{2})(\.?(\w|%[0-9A-Fa-f]{2}))*(:[1-9]\d{0,3}|\*)?))*})*$");
            if (formatName.EndsWith("-reference", StringComparison.Ordinal))
                return Uri.TryCreate(value, UriKind.Relative, out _);
            return Uri.TryCreate(value, UriKind.Absolute, out _);
        }
    }

    /// <summary>
    /// A class that validates whether a string value represents a valid host name or IP address.
    /// </summary>
    public class HostNameValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
        {
            if (formatName.EndsWith("hostname", StringComparison.Ordinal))
                return Uri.CheckHostName(value) != UriHostNameType.Unknown;
            var ipType = UriHostNameType.IPv4;
            if (formatName[^1] == '6')
                ipType = UriHostNameType.IPv6;
            var valueType = Uri.CheckHostName(value);
            return ipType == valueType;
        }
    }

    /// <summary>
    /// A class that validates whether a string value is a uuid.
    /// </summary>
    public class UuidValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
            => Guid.TryParseExact(value, "D", out _);
    }

    /// <summary>
    /// A class that validates whether a string value is a valid JSON pointer.
    /// </summary>
    public class JsonPointerValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
        {
            if (formatName.StartsWith("relative", StringComparison.Ordinal))
                return Regex.IsMatch(value, @"^(#|(\d|([1-9]\d*)))(\/(\w+|(""[^""]+"")|('[^']+)))+$");
            return Regex.IsMatch(value, @"^(\/(\w+|(""[^""]+"")|('[^']+)))+$");
        }
    }

    /// <summary>
    /// A class that validates whether a string value is a valid regular expression pattern.
    /// </summary>
    public class RegexValidator : IFormatValidator
    {
        public bool FormatIsValid(string formatName, string value)
        {
            try
            {
                _ = new Regex(value, RegexOptions.ECMAScript);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
