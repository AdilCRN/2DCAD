using MarkGeometriesLib.Classes.DXFParser;
using MSolvLib.MarkGeometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes.DRLParser
{
    public class DRLParser
    {
        private static readonly Regex MatchXValue = new Regex(@"X(\d+(\.\d+)?)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchYValue = new Regex(@"Y(\d+(\.\d+)?)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchEndOfFile = new Regex(@"^(\s)*(M30)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MatchDrillCommand = new Regex(@"^(\s)*(X|Y)", RegexOptions.Compiled | RegexOptions.Singleline);

        private long _count;
        private string _filePath;

        public long Count
        {
            get
            {
                return _count;
            }
        }

        public DRLParser(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                throw new FileNotFoundException($"Could not find `{filePathIn}`");

            _filePath = filePathIn;
        }

        public List<IMarkGeometry> ReadAll()
        {
            return Read(-1);
        }

        public List<IMarkGeometry> Read(long howmany = -1)
        {
            _count = 0;
            var buffer = new List<IMarkGeometry>();

            using (var reader = new AdvancedLineStreamReader(_filePath))
            {
                double x = 0;
                double y = 0;

                while ((howmany < 0 || (_count < howmany)) && !reader.EndOfStream)
                {
                    var (successful, line) = reader.FindLineUntil(MatchDrillCommand, MatchEndOfFile);

                    if (!successful)
                        break;

                    if (MatchXValue.IsMatch(line))
                    {
                        x = double.Parse(MatchXValue.Matches(line)[0].Groups[1].Value);
                    }

                    if (MatchYValue.IsMatch(line))
                    {
                        y = double.Parse(MatchYValue.Matches(line)[0].Groups[1].Value);
                    }

                    buffer.Add(
                        new MarkGeometryPoint(x, y)
                    );

                    _count++;
                }
            }

            return buffer;
        }
    }
}
