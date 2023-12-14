using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NyforvarvslistanFunction;

namespace func_nyforvarvslistan
{
    public class PublicationInfoExtractor
    {
        public static PublicationInfo Extract(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                var match = Regex.Match(input, @"Anpassad från: (.+?) : (.+?), (\d{4})\.");
                if (match.Success)
                {
                    return new PublicationInfo
                    {
                        City = match.Groups[1].Value.Trim(),
                        PublishingCompany = match.Groups[2].Value.Trim(),
                        PublishedYear = match.Groups[3].Value.Trim()
                    };
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
