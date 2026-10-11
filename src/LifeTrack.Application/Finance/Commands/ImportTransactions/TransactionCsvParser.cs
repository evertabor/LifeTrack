using System.Text;

namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public static class TransactionCsvParser
{
    public const int MaxRows = 500;

    public static IReadOnlyList<string[]> ParseRows(string content) =>
        new RowReader(content, DetectSeparator(content)).ReadAll();

    private static char DetectSeparator(string content)
    {
        foreach (var character in content)
        {
            if (character == ';')
            {
                return ';';
            }

            if (character == ',')
            {
                return ',';
            }

            if (character == '\n')
            {
                break;
            }
        }

        return ',';
    }

    private sealed class RowReader(string content, char separator)
    {
        private readonly List<string[]> _rows = [];
        private readonly List<string> _fields = [];
        private readonly StringBuilder _current = new();
        private bool _inQuotes;

        public IReadOnlyList<string[]> ReadAll()
        {
            for (var index = 0; index < content.Length; index++)
            {
                index = ReadCharacter(content, index);
            }

            FlushRow();
            return _rows;
        }

        private int ReadCharacter(string source, int index)
        {
            var character = source[index];

            if (_inQuotes)
            {
                return ReadQuoted(source, index, character);
            }

            if (character == '"')
            {
                _inQuotes = true;
                return index;
            }

            if (character == separator)
            {
                FlushField();
                return index;
            }

            if (character == '\r')
            {
                return index;
            }

            if (character == '\n')
            {
                FlushRow();
                return index;
            }

            _current.Append(character);
            return index;
        }

        private int ReadQuoted(string source, int index, char character)
        {
            if (character != '"')
            {
                _current.Append(character);
                return index;
            }

            if (index + 1 < source.Length && source[index + 1] == '"')
            {
                _current.Append('"');
                return index + 1;
            }

            _inQuotes = false;
            return index;
        }

        private void FlushField()
        {
            _fields.Add(_current.ToString());
            _current.Clear();
        }

        private void FlushRow()
        {
            FlushField();

            if (_fields.Count > 1 || _fields[0].Trim().Length > 0)
            {
                _rows.Add(_fields.ToArray());
            }

            _fields.Clear();
        }
    }
}
