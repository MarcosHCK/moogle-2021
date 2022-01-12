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

namespace Moogle.Engine
{
  public partial class Corpus : System.Object
  {
#region Variables
    public Dictionary<string, Word> Words {get; private set;}
    public Dictionary<GLib.IFile, Document> Documents {get; private set;}

#endregion

#region API

    public void Add (string word, long offset, GLib.IFile from)
    {
      Document? document = null;
      Word.Source? source = null;
      Word? store = null;

      do
      {
        if (!Documents.ContainsKey (from))
          Documents.Add (from, new Document ());
        else
        {
          document = Documents[from];
          if (!document.Words.ContainsKey (word))
            document.Words.Add (word, 1);
          else
            document.Words[word]++;
        }
      } while (document == null);

      do
      {
        if (!Words.ContainsKey(word))
        {
          /*
           * Append words and deviants
           *
           */

          int length = word.Length;
          var store_ = new Word();
          Words.Add (word, store_);

          for (length -= 1; length > 1; length--)
          {
            Words.TryAdd (word.Substring (0, length), store_);
          }
        }
        else
        {
          store = Words[word];
          store.Occurrences++;

          do
          {
            if(!store.Locations.ContainsKey(document))
              store.Locations.Add (document, new Word.Source ());
            else
            {
              source = store.Locations[document];
              source.Offsets.Add (offset);
            }
          } while (source == null);
        }
      } while (store == null);
    }

#endregion

#region Operations

    public static double Tf (Word word, Document document)
    {
      Word.Source? source;
      if (word.Locations.TryGetValue (document, out source))
        return Math.Log ((double) source.Offsets.Count);
    return 0d;
    }

    public static double Tf (string word, Document document)
    {
      long count;
      if (document.Words.TryGetValue(word, out count))
        return Math.Log ((double) count) + 1d;
    return 0d;
    }

    public static double Idf (string word, Corpus corpus)
    {
      Word store;
      if(corpus.Words.TryGetValue(word, out store!))
      {
        decimal docs = (decimal) store.Locations.Keys.Count;
        return Math.Log ((double) (docs / (store.Occurrences)));
      }
    return 0d;
    }

    public static double Tfidf (string word, Document document, Corpus corpus)
    {
      return Tf (word, document) * Idf (word, corpus);
    }

#endregion

#region Constructor

    public Corpus()
    {
      this.Documents = new Dictionary<GLib.IFile, Document>();
      this.Words = new Dictionary<string, Word>();
    }

#endregion
  }
}
