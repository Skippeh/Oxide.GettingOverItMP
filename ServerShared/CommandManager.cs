using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerShared
{
    public class CommandManager
    {
        private GameServer server;

        public CommandManager(GameServer server)
        {
            this.server = server;
        }

        public void HandleChatMessage(string message)
        {
            string[] words = message.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            string command = words[0];
            string[] args = ParseArgs(words.Skip(1));

            /*Console.WriteLine($"Raw: {message}");
            Console.WriteLine($"Raw args: '{string.Join("', '", words.Skip(1).ToArray())}'");
            Console.WriteLine($"Command: \"{command}\"");
            Console.WriteLine($"Args ({args.Length}): '{string.Join("', '", args)}'");*/
        }

        private string[] ParseArgs(IEnumerable<string> wordsEnumerable)
        {
            var result = new List<string>();
            string[] words = wordsEnumerable.ToArray();

            StringBuilder currentWord = null;
            for (int i = 0; i < words.Length; ++i)
            {
                string word = words[i];
                string cleanWord = word.Replace("\"", "");

                if (cleanWord == string.Empty && currentWord == null)
                {
                    result.Add(cleanWord);
                    continue;
                }

                if (currentWord == null)
                {
                    currentWord = new StringBuilder(cleanWord);

                    if (!word.StartsWith("\""))
                    {
                        result.Add(currentWord.ToString());
                        currentWord = null;
                    }
                }
                else
                {
                    currentWord.Append(" " + cleanWord);

                    if (word.EndsWith("\""))
                    {
                        result.Add(currentWord.ToString());
                        currentWord = null;
                    }
                }
            }

            return result.ToArray();
        }
    }
}
