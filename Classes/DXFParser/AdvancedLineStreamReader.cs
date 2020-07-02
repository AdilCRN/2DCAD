using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes.DXFParser
{
    public class AdvancedLineStreamReader : StreamReader
    {
        private readonly Queue<string> _readQueue = new Queue<string>();

        public AdvancedLineStreamReader(Stream stream) : base(stream)
        {
        }

        public AdvancedLineStreamReader(string path) : base(path)
        {
        }

        public AdvancedLineStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks) : base(stream, detectEncodingFromByteOrderMarks)
        {
        }

        public AdvancedLineStreamReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public AdvancedLineStreamReader(string path, bool detectEncodingFromByteOrderMarks) : base(path, detectEncodingFromByteOrderMarks)
        {
        }

        public AdvancedLineStreamReader(string path, Encoding encoding) : base(path, encoding)
        {
        }

        public AdvancedLineStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks) : base(stream, encoding, detectEncodingFromByteOrderMarks)
        {
        }

        public AdvancedLineStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks) : base(path, encoding, detectEncodingFromByteOrderMarks)
        {
        }

        public AdvancedLineStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
        }

        public AdvancedLineStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
        }

        public AdvancedLineStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen) : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen)
        {
        }

        /// <summary>
        /// Reads the next line from the current stream.
        /// Ignores all lines in the peek buffer.
        /// </summary>
        /// <returns>Returns the next line from the current stream ignoring all lines in the peek buffer</returns>
        public string BaseReadLine()
        {
            return base.ReadLine();
        }

        /// <summary>
        /// Reads lines from the current stream until the target is found.
        /// </summary>
        /// <param name="targetRegexIn">A regex defining the target</param>
        /// <returns>Returns the search status and the line if successful</returns>
        public (bool Success, string Line) FindLine(Regex targetRegexIn)
        {
            string line;

            do
            {
                line = PeekLine();

                if (targetRegexIn.IsMatch(line))
                {
                    // consume line
                    ReadLine();
                    return (true, line);
                }
                else // advance to next line
                    ReadLine();
            }
            while (line != null && !EndOfStream);

            return (false, null);
        }

        /// <summary>
        /// Reads lines from the current stream until the target is found.
        /// </summary>
        /// <param name="targetRegexIn">Text defining the target</param>
        /// <returns>Returns the search status and the line if successful</returns>
        public (bool Success, string Line) FindLine(string targetIn)
        {
            string line;

            do
            {
                line = PeekLine().Trim();

                if (line == targetIn)
                {
                    // consume line
                    ReadLine();
                    return (true, line);
                }
                else // advance to next line
                    ReadLine();
            }
            while (line != null && !EndOfStream);

            return (false, null);
        }

        /// <summary>
        /// Reads lines from the current stream until the target or termination regex is found.
        /// </summary>
        /// <param name="targetRegexIn">A regex defining the target</param>
        /// <param name="terminationRegexIn">A regex defining the end</param>
        /// <returns>Returns the search status and the line if successful</returns>
        public (bool Success, string Line) FindLineUntil(Regex targetRegexIn, Regex terminationRegexIn)
        {
            string line;

            do
            {
                line = PeekLine();

                if (targetRegexIn.IsMatch(line))
                {
                    // consume line
                    ReadLine();
                    return (true, line);
                }
                else if (terminationRegexIn.IsMatch(line))
                {
                    // consume line
                    ReadLine();
                    return (false, line);
                }
                else // advance to next line
                    ReadLine();
            }
            while (line != null && !EndOfStream);

            return (false, null);
        }

        /// <summary>
        /// Reads lines from the current stream until the target or termination text is found.
        /// </summary>
        /// <param name="targetIn">A text defining the target</param>
        /// <param name="terminationTextIn">A text defining the end</param>
        /// <returns>Returns the search status and the line if successful</returns>
        public (bool Success, string Line) FindLineUntil(string targetIn, string terminationTextIn)
        {
            string line;

            do
            {
                line = PeekLine().Trim();

                if (line == targetIn)
                {
                    // consume line
                    ReadLine();
                    return (true, line);
                }
                else if (line == terminationTextIn)
                {
                    // consume line
                    ReadLine();
                    return (false, line);
                }
                else // advance to next line
                    ReadLine();
            }
            while (line != null && !EndOfStream);

            return (false, null);
        }

        /// <summary>
        /// Reads lines from the current stream until the target consecutive lines are found.
        /// </summary>
        /// <param name="lineBTargetRegex">A regex defining the target in line A</param>
        /// <param name="lineBTargetRegex">A regex defining the target in line B</param>
        /// <returns>Returns the search status and the lines if successful</returns>
        public (bool Success, string LineA, string LineB) FindConsecutiveLines(Regex lineATargetRegex, Regex lineBTargetRegex)
        {
            string line;
            string nextLine;

            while (!EndOfStream)
            {
                line = ReadLine();
                nextLine = PeekLine();

                if (line == null || nextLine == null)
                {
                    return (false, null, null);
                }
                else if (lineATargetRegex.IsMatch(line) && lineBTargetRegex.IsMatch(nextLine))
                {
                    // consume line
                    ReadLine();
                    return (true, line, nextLine);
                }
                else
                {
                    // advance to next line
                    ReadLine();
                }
            }

            return (false, null, null);
        }

        /// <summary>
        /// Reads lines from the current stream until the target consecutive lines are found.
        /// </summary>
        /// <param name="lineBTargetRegex">A text defining the target in line A</param>
        /// <param name="lineBTargetRegex">A text defining the target in line B</param>
        /// <returns>Returns the search status and the lines if successful</returns>
        public (bool Success, string LineA, string LineB) FindConsecutiveLines(string lineATarget, string lineBTarget)
        {
            string line;
            string nextLine;

            while (!EndOfStream)
            {
                line = ReadLine()?.Trim();
                nextLine = PeekLine()?.Trim();

                if (line == null || nextLine == null)
                {
                    return (false, null, null);
                }
                else if (lineATarget == line && lineBTarget == nextLine)
                {
                    // consume line
                    ReadLine();
                    return (true, line, nextLine);
                }
                else
                {
                    // advance to next line
                    ReadLine();
                }
            }

            return (false, null, null);
        }

        /// <summary>
        /// Reads the next line from the current stream.
        /// </summary>
        /// <returns>Returns the next line from the current stream</returns>
        public override string ReadLine()
        {
            if (_readQueue.Count > 0)
                return _readQueue.Dequeue();

            return base.ReadLine();
        }

        /// <summary>
        /// Removes all lines in the peek queue.
        /// </summary>
        public void ClearPeekQueue()
        {
            _readQueue.Clear();
        }

        /// <summary>
        /// Returns the next line without removing it.
        /// </summary>
        /// <returns>Returns the next line without removing it</returns>
        public string PeekLine()
        {
            if (_readQueue.Count > 0)
                return _readQueue.Peek();

            string line = BaseReadLine();

            if (line != null)
                _readQueue.Enqueue(line);

            return line;
        }
    }
}
