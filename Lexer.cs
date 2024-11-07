using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KatrinEngine.KatrinLang
{
    public class Lexer
    {
        private readonly string _input;
        private int _position;
        private int _line;
        private int _column;

        // Регулярные выражения для определения токенов
        private static readonly Regex _stringLiteralRegex = new("\"([^\"]*)\"", RegexOptions.Compiled);
        private static readonly Regex _numberLiteralRegex = new(@"(\d+)", RegexOptions.Compiled);
        private static readonly Regex _identifierRegex = new(@"[a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.Compiled);

        // Ключевые слова для языка визуальных новелл
        private static readonly Dictionary<string, TokenType> Keywords = new()
        {
            { "say", TokenType.Keyword },
            { "choice", TokenType.Keyword },
            { "if", TokenType.Keyword },
            { "else", TokenType.Keyword },
            { "show", TokenType.Keyword },
            { "hide", TokenType.Keyword },
            { "background", TokenType.Keyword },
            { "scene", TokenType.Keyword },
            { "end", TokenType.Keyword },
            { "character", TokenType.Keyword },
            // Добавьте сюда любые другие ключевые слова, которые вам нужны
        };

        // Специальные ключевые слова для инициализации ресурсов
        private static readonly Dictionary<string, TokenType> ResourceKeywords = new()
        {
            { "Assets", TokenType.Keyword },
        };

        public Lexer(string input)
        {
            _input = input;
            _position = 0;
            _line = 1;
            _column = 1;
        }

        public Token NextToken()
        {
            if (_position >= _input.Length)
                return new Token(TokenType.EndOfFile, "", _line, _column);

            SkipWhitespace();

            if (_position >= _input.Length)
                return new Token(TokenType.EndOfFile, "", _line, _column);

            char currentChar = _input[_position];

            // Проверка ключевых слов и идентификаторов
            if (char.IsLetter(currentChar) || currentChar == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            // Проверка строковых литералов
            if (currentChar == '"')
            {
                return ReadStringLiteral();
            }

            // Проверка числовых литералов
            if (char.IsDigit(currentChar))
            {
                return ReadNumberLiteral();
            }

            // Обработка операторов и символов
            switch (currentChar)
            {
                case '\n':
                    _line++;
                    _column = 1;
                    _position++;
                    return NextToken(); // An implicit return here to skip over newlines
                default:
                    Token unknownToken = new(TokenType.Unknown, currentChar.ToString(), _line, _column);
                    _position++;
                    _column++;
                    return unknownToken;
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                if (_input[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }

        private Token ReadIdentifierOrKeyword()
        {
            var start = _position;
            while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
            {
                _position++;
                _column++;
            }

            string value = _input[start.._position];
            TokenType type = Keywords.TryGetValue(value, out var tokenType) ? tokenType : TokenType.Identifier;

            return new Token(type, value, _line, _column);
        }

        private Token ReadStringLiteral()
        {
            Match match = _stringLiteralRegex.Match(_input, _position);
            if (match.Success)
            {
                _position += match.Length;
                _column += match.Length;
                return new Token(TokenType.String, match.Groups[1].Value, _line, _column);
            }

            return new Token(TokenType.Unknown, "", _line, _column);
        }

        private Token ReadNumberLiteral()
        {
            Match match = _numberLiteralRegex.Match(_input, _position);
            if (match.Success)
            {
                _position += match.Length;
                _column += match.Length;
                return new Token(TokenType.Number, match.Value, _line, _column);
            }

            return new Token(TokenType.Unknown, "", _line, _column);
        }
    }
}
