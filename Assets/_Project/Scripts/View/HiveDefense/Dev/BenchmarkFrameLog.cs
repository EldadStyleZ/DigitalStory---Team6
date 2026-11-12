using System;
using System.IO;

namespace DigitalStory.View
{
    /// <summary>
    /// Development tool, not game code. Builds the benchmark's CSV in memory without allocating, one row a frame, and
    /// writes it to disk only when told to, between measured windows, so file writes never land in a measured frame.
    /// </summary>
    internal sealed class BenchmarkFrameLog : IDisposable
    {
        private const int BufferCharacters = 8 * 1024 * 1024;
        private const int FlushShare = 2; // Write early once half the buffer is used, even inside a window.
        private const int NumberCharacters = 24;
        private const int MostDecimals = 6;

        private static readonly long[] PowersOfTen = { 1, 10, 100, 1000, 10000, 100000, 1000000 };

        private readonly StreamWriter _writer;
        private readonly char[] _buffer = new char[BufferCharacters];
        private readonly char[] _digits = new char[NumberCharacters];
        private int _length;
        private bool _isLineStart = true;

        public BenchmarkFrameLog(string path)
        {
            _writer = new StreamWriter(path, false);
        }

        /// <summary>True when the buffer is half full, so the caller should write it out even inside a window.</summary>
        public bool IsFilling => _length > BufferCharacters / FlushShare;

        /// <summary>Adds a text field.</summary>
        public void Text(string value)
        {
            Separate();
            if (value == null)
            {
                return;
            }

            value.CopyTo(0, _buffer, _length, value.Length);
            _length += value.Length;
        }

        /// <summary>Adds an empty field.</summary>
        public void Empty() => Separate();

        /// <summary>Adds a whole number.</summary>
        public void Integer(long value)
        {
            Separate();
            WriteInteger(value);
        }

        /// <summary>Adds a number with a fixed count of decimals, rounded half away from zero.</summary>
        public void Fixed(double value, int decimals)
        {
            Separate();
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return;
            }

            decimals = Math.Max(0, Math.Min(decimals, MostDecimals));
            long scale = PowersOfTen[decimals];
            long scaled = (long)Math.Round(Math.Abs(value) * scale, MidpointRounding.AwayFromZero);
            if (value < 0.0 && scaled > 0)
            {
                _buffer[_length++] = '-';
            }

            WriteInteger(scaled / scale);
            if (decimals == 0)
            {
                return;
            }

            _buffer[_length++] = '.';
            long fraction = scaled % scale;
            for (long place = scale / 10; place >= 1; place /= 10)
            {
                _buffer[_length++] = (char)('0' + ((fraction / place) % 10));
            }
        }

        /// <summary>Ends the row.</summary>
        public void EndRow()
        {
            _buffer[_length++] = '\n';
            _isLineStart = true;
        }

        /// <summary>Writes every finished row to the file.</summary>
        public void Flush()
        {
            if (_length == 0)
            {
                return;
            }

            _writer.Write(_buffer, 0, _length);
            _writer.Flush();
            _length = 0;
        }

        public void Dispose()
        {
            Flush();
            _writer.Dispose();
        }

        private void Separate()
        {
            if (!_isLineStart)
            {
                _buffer[_length++] = ',';
            }

            _isLineStart = false;
        }

        private void WriteInteger(long value)
        {
            if (value < 0)
            {
                _buffer[_length++] = '-';
                value = -value;
            }

            int count = 0;
            do
            {
                _digits[count++] = (char)('0' + (value % 10));
                value /= 10;
            }
            while (value > 0);

            while (count > 0)
            {
                _buffer[_length++] = _digits[--count];
            }
        }
    }
}
