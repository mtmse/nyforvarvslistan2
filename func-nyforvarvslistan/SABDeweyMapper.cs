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
        private readonly Dictionary<string, string> deweyToSabMap = new Dictionary<string, string>();

        // Constructor for file path input (existing behavior)
        public SABDeweyMapper(string filePath, bool isFilePath)
        {
            if (isFilePath)
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    ProcessLine(line);
                }
            }
        }

        // Constructor for file content input (new behavior)
        public SABDeweyMapper(string fileContent)
        {
            using (var reader = new StringReader(fileContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    ProcessLine(line);
                }
            }
        }

        // Common method to process a line and populate the map
        private void ProcessLine(string line)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 4)
            {
                var key = parts[2];
                if (!deweyToSabMap.ContainsKey(key))
                {
                    deweyToSabMap[key] = parts[1];
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
