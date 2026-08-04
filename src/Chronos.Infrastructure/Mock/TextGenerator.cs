using System;
using System.Linq;
using System.Text;

namespace Chronos.Infrastructure.Mock
{
    internal static class TextGenerator
    {
        public static string Create(
            int minWords = 5,
            int maxWords = 10,
            int minLetters = 5,
            int maxLetters = 15)
        {
            Random random = new Random();
            var builder = new StringBuilder();
            var wordsCount = random.Next(minWords, maxWords);
            char[] lowers = Enumerable.Range(0, 32).Select((x, i) => (char)('а' + i)).ToArray();
            char[] uppers = Enumerable.Range(0, 32).Select((x, i) => (char)('А' + i)).ToArray();
            for (int i = 0; i < wordsCount; i++)
            {
                string word = string.Empty;
                var lettersCount = random.Next(minLetters, maxLetters);
                for (int j = 0; j < lettersCount; j++)
                {
                    int letterPosition = random.Next(0, lowers.Length - 1);
                    word += lowers[letterPosition];
                }

                builder.Append(word);
                builder.Append(' ');
            }

            return builder.ToString();
        }
    }
}
