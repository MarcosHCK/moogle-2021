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
      [Operator.Glyph (Glyph = '~', Single = true)]
      public class ProximityOperator : Operator
      {
        private class MoreCapture : Operator.Capture
        {
          public string? other;
        }

        private static Match? GetPrev (Match target, Match first)
        {
          var hash = target.Index;
          Match? match = first;
          Match? prev = null;

          do
          {
            if (match.Index == hash)
              return prev;
            if (!match.Success)
              return null;

            prev = match;
            match = match.NextMatch ();
          }
          while (true);
        }

        private static Match? GetNext (Match target, Match first)
        {
          Match? match = target.NextMatch ();
        return (match.Success) ? match : null;
        }

        public override string? BeginCapture (ref Capture? context, Match first, Match match)
        {
          context = null;
          string word = match.Value;

          if (word[0] == '~'
            && word.Length == 1)
          {
            var one = GetPrev (match, first);
            if (one == null)
              return null;
            var two = GetNext (match, first);
            if (two == null)
              return null;

            var
            context_ = new MoreCapture ();
            context_.instance = one.Value;
            context_.other = two.Value;
            context_.emit = false;
            context = context_;
            return word;
          }
          return null;
        }

        private long SearchShortest (long[] set1, long[] set2)
        {
          var length1 = set1.Length;
          var length2 = set2.Length;
          long shortest = long.MaxValue;
          int i;

          long CloserToPoint (long point, long offset, int length)
          {
            if (length == 1)
              return Math.Abs (set1[offset] - point);
            var half = length / 2;
            if (set1[offset + half] > point)
              return CloserToPoint (point, offset, half);
            else
              return CloserToPoint (point, offset + half, length - half);
          }

          for (i = 0; i < length2; i++)
          {
            long point = set2[i];
            long result = CloserToPoint (point, 0, length1);
            if (result < shortest)
              shortest = result;
          }
        return shortest;
        }

        public override Filter? EndCapture (ref Capture? context)
        {
          if (context != null)
          {
            var moreCapture = (MoreCapture) context;
            return (query, corpus, vector, score) =>
            {
              var first = moreCapture.instance;
              var last = moreCapture.other!;
              if (vector.Words.ContainsKey (first)
                && vector.Words.ContainsKey (last))
                {
                  var set1 = corpus.Words[first].Locations[vector].Offsets;
                  var set2 = corpus.Words[last].Locations[vector].Offsets;
                  var distance = SearchShortest (set1.ToArray (), set2.ToArray ());
                  var factor = ((double) distance) / ((double) vector.Words.Count);
                  return score / factor;
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
