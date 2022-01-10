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
using System.Collections;

namespace Moogle.Engine
{
  public class Corpus : System.Object, IEnumerable<KeyValuePair<string, Corpus.Word>>
  {
#region Variables
    private Dictionary<string, Word> words;

#endregion

#region Arcilliary classes

    public class Word
    {
      public decimal occurrences;
      public Dictionary<GLib.IFile, Source> locations;

      public class Source
      {
        public List<decimal> offsets;

        public Source()
        {
          this.offsets = new List<decimal>();
        }
      }

      public Word()
      {
        this.locations =
        new Dictionary<GLib.IFile, Source>();
        this.occurrences = 0;
      }
    }

#endregion

#region API

    public void Add (string word, decimal offset, GLib.IFile from)
    {
      Word.Source? source = null;
      Word? ctx = null;

      do
      {
        if (words.ContainsKey (word))
        {
          ctx = words[word];
          ctx.occurrences++;

          do
          {
            if (ctx.locations.ContainsKey (from))
            {
              source = ctx.locations[from];
              source.offsets.Add (offset);
            }
            else
            {
              ctx.locations.Add (from, new Word.Source ());
            }
          } while (source == null);
        }
        else
        {
          words.Add (word, new Word ());
        }
      } while (ctx == null);
    }

#endregion

#region IEnumerable

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, Word>> GetEnumerator() => words.GetEnumerator();

#endregion

#region Constructor

    public Corpus()
    {
      this.words = new Dictionary<string, Word>();
    }

#endregion
  }
}
