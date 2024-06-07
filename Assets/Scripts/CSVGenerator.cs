using System.IO;
using System.Text;

namespace Logger
{
    public class CSVGenerator
    {
        public bool Exported { get; private set; }
        readonly StringBuilder content;

        public CSVGenerator(params string[] headers)
        {
            content = new();

            content.Append(headers[0]);

            for (int i = 1; i < headers.Length; i++)
            {
                content.Append($",{headers[i]}");
            }
        }

        public void AppendRow(params string[] row)
        {
            content.AppendLine();

            content.Append(row[0]);
            for (int i = 1; i < row.Length; i++)
            {
                content.Append($",{row[i]}");
            }
        }

        public void Export(string folderPath, string fileName)
        {
            Exported = true;
            int count = 0;
            while (File.Exists(ToPath(folderPath, fileName, count)))
            {
                count++;
            }
            using StreamWriter sw = new(ToPath(folderPath, fileName, count));
            sw.Write(content.ToString());
        }

        string ToPath(string folder, string fileName, int num)
        {
            return Path.Combine(folder, $"{fileName}({num}).csv");
        }
    }
}
