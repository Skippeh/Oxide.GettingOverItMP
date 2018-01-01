using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ServerShared.Logging;

namespace Server
{
    public static class ConsoleManager
    {
        private static Thread inputThread;
        private static string currentInput = string.Empty;

        private static readonly Stack<ConsoleColor> foregroundColorStack = new Stack<ConsoleColor>();
        private static readonly Stack<ConsoleColor> backgroundColorStack = new Stack<ConsoleColor>();
        
        public static void Initialize()
        {
            Logger.LogMessageReceived += OnLogMessageReceived;
            inputThread = new Thread(DoInputThread);
            inputThread.Start();

            PushForegroundColor(ConsoleColor.DarkGreen);
        }

        public static void Destroy()
        {
            Logger.LogMessageReceived -= OnLogMessageReceived;
            inputThread.Abort();
            PopForegroundColor();
        }

        private static void OnLogMessageReceived(LogMessageReceivedEventArgs args)
        {
            switch (args.Type)
            {
                case LogMessageType.Info:
                    WriteLine(args.Message);
                    break;
                case LogMessageType.Debug:
                    PushForegroundColor(ConsoleColor.Yellow);
                    WriteLine(args.Message);
                    PopForegroundColor();
                    break;
                case LogMessageType.Warning:
                    PushForegroundColor(ConsoleColor.DarkYellow);
                    WriteLine(args.Message);
                    PopForegroundColor();
                    break;
                case LogMessageType.Error:
                    PushForegroundColor(ConsoleColor.Red);
                    WriteLine(args.Message);
                    PopForegroundColor();
                    break;
                case LogMessageType.Exception:
                    PushForegroundColor(ConsoleColor.Red);

                    if (args.Message != null)
                        WriteLine(args.Message);

                    WriteException(args.Exception);

                    PopForegroundColor();
                    break;
            }
        }

        private static void WriteLine(object msg)
        {
            ClearLines(1);
            Console.WriteLine(msg);
            
            RedrawInput();
        }

        private static void WriteException(Exception exception)
        {
            ClearLines(1);
            Console.WriteLine(exception);

            if (exception.InnerException != null)
            {
                PushForegroundColor(ConsoleColor.DarkRed);
                Console.Write("Inner: ");
                PopForegroundColor();
                WriteException(exception.InnerException);
            }
            
            RedrawInput();
        }

        private static void DoInputThread()
        {
            while (true)
            {
                var keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    OnBackspace();
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    OnEnter();
                }
                else if (keyInfo.KeyChar != 0)
                {
                    currentInput += keyInfo.KeyChar;
                }
                
                RedrawInput();
            }
        }

        private static void OnBackspace()
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
            }
        }

        private static void OnEnter()
        {
            if (currentInput.Length <= 0)
                return;

            PushForegroundColor(ConsoleColor.Green);
            WriteLine($"{currentInput}");
            PopForegroundColor();

            // Todo: process input
            OnLogMessageReceived(new LogMessageReceivedEventArgs(LogMessageType.Warning, "Unknown command."));

            currentInput = string.Empty;
        }

        private static void RedrawInput()
        {
            Console.CursorVisible = false; // Prevent flickering cursor
            ClearLines(2);

            string input = currentInput;

            // Negate 2 to account for the > at the start and leaving 1 empty cell at the end of the line.
            if (input.Length > Console.BufferWidth - 1)
            {
                input = input.Substring(input.Length - (Console.BufferWidth - 1));
            }

            PushForegroundColor(ConsoleColor.Green);
            Console.Write($"{input}");
            PopForegroundColor();
            Console.CursorVisible = true;
        }

        private static void ClearLines(int numLines)
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.BufferWidth * numLines));
            Console.CursorTop -= numLines;
        }

        private static void PushForegroundColor(ConsoleColor color)
        {
            foregroundColorStack.Push(Console.ForegroundColor);
            Console.ForegroundColor = color;
        }

        private static void PopForegroundColor()
        {
            Console.ForegroundColor = foregroundColorStack.Pop();
        }

        private static void PushBackgroundColor(ConsoleColor color)
        {
            backgroundColorStack.Push(Console.BackgroundColor);
            Console.BackgroundColor = color;
        }

        private static void PopBackgroundColor()
        {
            Console.BackgroundColor = backgroundColorStack.Pop();
        }
    }
}
