using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan
{
    public class SABDeweyMapper
    {
        private Dictionary<string, string> deweyToSabMap = new Dictionary<string, string>();

        public SABDeweyMapper(string filePath)
        {
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('\t');
                if (parts.Length >= 4)
                {
                    deweyToSabMap[parts[2]] = parts[1];
                }
            }
        }
        public string getSabCode(string deweyCode)
        {
            if (deweyToSabMap.ContainsKey(deweyCode))
            {
                return deweyToSabMap[deweyCode];
            }
            while (deweyCode.Length > 0)
            {
                deweyCode = deweyCode.Remove(deweyCode.Length - 1);
                if (deweyToSabMap.ContainsKey(deweyCode))
                {
                    return deweyToSabMap[deweyCode];
                }
            }
            return null;
        }
    }
}
