/* Copyright 2021-2025 MarcosHCK
 * This file is part of Moogle!.
 *
 * Moogle! is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Moogle! is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Moogle!. If not, see <http://www.gnu.org/licenses/>.
 *
 */
using System.Text.RegularExpressions;

namespace Moogle.Engine
{
  public partial class Corpus
  {
    public partial class Query
    {
      [Operator.Glyph (Glyph = '*')]
      public class ImportanceOperator : Operator
      {
        private class MoreCapture : Operator.Capture
        {
          public int importance;
        }

        public override string? BeginCapture (ref Capture? context, Match first, Match current)
        {
          context = null;
          string word = current.Value;

          if (word[0] == '*')
          {
            int i, exp = 1;
            for (i = 1; i < word.Length && word[i] == '*'; i++)
              exp++;
            word = word.Substring (exp);

            var
            context_ = new MoreCapture();
            context_.instance = word;
            context_.importance = exp;
            context = context_;
            return word;
          }
          return null;
        }

        public override Filter? EndCapture (ref Capture? context)
        {
          if (context != null)
          {
            var word = ((Capture) context!).instance;
            var reps = ((MoreCapture) context!).importance;
            return (query, corpus, vector, score) =>
            {
              if (vector.Words.ContainsKey (word))
                {
                  var idf = Corpus.Idf (word, corpus);
                  for (int i = 0; i < reps; i++)
                    score *= idf;
                }
              return score;
            };
          }
          return null;
        }
      }
    }
  }
}
