using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KatrinEngine.KatrinLang
{
    // Класс парсера
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _currentTokenIndex;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _currentTokenIndex = 0;
        }

        public KatrinGame Parse()
        {
            // Создание объекта KatrinGame
            KatrinGame game = new KatrinGame();

            // Парсинг всех инструкций в программе
            while (_currentTokenIndex < _tokens.Count)
            {
                game.Instructions.Add(ParseInstruction());
            }

            return game;
        }

        // Метод для парсинга инструкции
        private Instruction ParseInstruction()
        {
            Token currentToken = Peek();

            // Парсинг "init"
            if (currentToken.Type == TokenType.Assets)
            {
                return ParseInitInstruction();
            }

            // Парсинг "call"
            if (currentToken.Type == TokenType.Call)
            {
                return ParseCallInstruction();
            }

            // Парсинг "background"
            if (currentToken.Type == TokenType.Background)
            {
                return ParseBackgroundInstruction();
            }

            // Парсинг "play"
            if (currentToken.Type == TokenType.Play)
            {
                return ParsePlayInstruction();
            }

            // Парсинг "say"
            if (currentToken.Type == TokenType.Say)
            {
                return ParseSayInstruction();
            }

            // Парсинг "wait"
            if (currentToken.Type == TokenType.Wait)
            {
                return ParseWaitInstruction();
            }

            // Парсинг "end"
            if (currentToken.Type == TokenType.End)
            {
                return ParseEndInstruction();
            }

            // Парсинг "load_script"
            if (currentToken.Type == TokenType.LoadScript)
            {
                return ParseLoadScriptInstruction();
            }

            throw new Exception($"Неизвестная инструкция: {currentToken.Value}");
        }

        // Методы для парсинга различных инструкций:

        private Instruction ParseInitInstruction()
        {
            // Проверка наличия "init"
            Expect(TokenType.Assets);
            Expect(TokenType.LeftBrace);

            List<string> parameters = new List<string>();

            // Парсинг инструкций внутри "init"
            while (Peek().Type != TokenType.RightBrace)
            {
                parameters.Add(Peek().Value);
                Next();
            }

            // Проверка наличия "}"
            Expect(TokenType.RightBrace);

            return new Instruction(InstructionType.Assets, "Assets", parameters);
        }

        private Instruction ParseCallInstruction()
        {
            // Проверка наличия "call"
            Expect(TokenType.Call);
            // Получение имени действия
            string actionName = Expect(TokenType.Identifier).Value;
            // Получение параметров действия
            List<string> parameters = new List<string>();
            if (Peek().Type == TokenType.LeftParen)
            {
                Expect(TokenType.LeftParen);
                while (Peek().Type != TokenType.RightParen)
                {
                    parameters.Add(Expect(TokenType.Identifier).Value);
                    if (Peek().Type == TokenType.Comma)
                    {
                        Expect(TokenType.Comma);
                    }
                }
                Expect(TokenType.RightParen);
            }

            return new Instruction(InstructionType.Call, actionName, parameters);
        }

        private Instruction ParseBackgroundInstruction()
        {
            // Проверка наличия "background"
            Expect(TokenType.Background);
            // Получение имени фона
            string backgroundName = Expect(TokenType.Identifier).Value;

            return new Instruction(InstructionType.Background, backgroundName);
        }

        private Instruction ParsePlayInstruction()
        {
            // Проверка наличия "play"
            Expect(TokenType.Play);
            // Получение имени музыки
            string musicName = Expect(TokenType.Identifier).Value;

            return new Instruction(InstructionType.Play, musicName);
        }

        private Instruction ParseSayInstruction()
        {
            // Проверка наличия "say"
            Expect(TokenType.Say);
            // Получение текста диалога
            string text = Expect(TokenType.String).Value;

            return new Instruction(InstructionType.Say, text);
        }

        private Instruction ParseWaitInstruction()
        {
            // Проверка наличия "wait"
            Expect(TokenType.Wait);
            // Получение времени ожидания
            string waitTime = Expect(TokenType.Integer).Value;

            return new Instruction(InstructionType.Wait, waitTime);
        }

        private Instruction ParseEndInstruction()
        {
            // Проверка наличия "end"
            Expect(TokenType.End);

            return new Instruction(InstructionType.End, "end");
        }

        private Instruction ParseLoadScriptInstruction()
        {
            // Проверка наличия "load_script"
            Expect(TokenType.LoadScript);
            // Получение пути к .kat файлу
            string scriptPath = Expect(TokenType.String).Value;

            return new Instruction(InstructionType.LoadScript, scriptPath);
        }

        // Вспомогательные методы

        // Возвращает текущий токен, не удаляя его из потока
        private Token Peek()
        {
            if (_currentTokenIndex < _tokens.Count)
            {
                return _tokens[_currentTokenIndex];
            }

            return null;
        }

        // Возвращает текущий токен и перемещает указатель на следующий
        private Token Next()
        {
            if (_currentTokenIndex < _tokens.Count)
            {
                return _tokens[_currentTokenIndex++];
            }

            return null;
        }

        // Ожидает токен определенного типа и перемещает указатель на следующий
        private Token Expect(TokenType type)
        {
            Token currentToken = Next();
            if (currentToken.Type != type)
            {
                throw new Exception($"Ожидался токен типа {type}, но получен {currentToken.Type}");
            }
            return currentToken;
        }
    }
}
