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
  public class Corpus : IEnumerable, ICollection
  {
#region Variables
    private Hashtable documents = new Hashtable();
    private Hashtable idfs = new Hashtable();
#endregion

#region API

    public Document? Add(GLib.IFile file, GLib.FileInfo info, GLib.Cancellable? cancellable = null)
    {
      var key = file.ParsedName;
      if (documents.Contains(key))
        return ((Document?) documents[key]);
      else
      {
        var document = DocumentLoader.Load(file, info.ContentType, cancellable);
        if (document != null)
          documents.Add(key, document);
        return document;
      }
    }

    public void Remove(Document document)
    {
      foreach(DictionaryEntry entry in documents)
      {
        if(entry.Value == document)
        {
          documents.Remove(entry.Key);
          foreach (string word in document)
            idfs.Remove(word);
          return;
        }
      }
    }

    public void Update(GLib.Cancellable? cancellable = null)
    {
      List<string>? todelete = null;
      foreach (DictionaryEntry entry in documents)
      {
        try
        {
          ((Document) entry.Value!).Update(cancellable);

          if (cancellable != null
            && cancellable.IsCancelled)
            return;
        }
        catch (GLib.GException e)
        {
          if(e.Domain == GLib.GioGlobal.ErrorQuark()
            && e.Code == (int) GLib.IOErrorEnum.NotFound)
          {
            /* Document got deleted or otherwise unavailable */
            if (todelete == null)
              todelete = new List<string>();
            todelete.Add((string) entry.Key);
          }
        }
      }

      if (todelete != null)
      {
        foreach (string key in todelete)
          documents.Remove(key);
      }
    }

#endregion

#region ICollection

    public int Count {
      get {
        return documents.Values.Count;
      }}
    public bool IsSynchronized {
      get {
        return false;
      }}
    public object SyncRoot {
      get {
        return (object) this;
      }}

    public void CopyTo(Array array, int index) => documents.Values.CopyTo(array, index);

#endregion

#region IEnumerable

    public IEnumerator GetEnumerator() => documents.Values.GetEnumerator();

#endregion

#region TF-IDF functions

    public static double Tf(string word, Document document)
    {
      decimal count = document[word];
      if (count == 0)
        return 0;
      else
      {
        return Math.Log((double) (count)) + 1d;
      }
    }

    private static double Idf_(string word, Corpus corpus)
    {
      /* Count wor occurrencies */
      decimal globalCount = 0;
      var documents = corpus.documents;

      foreach (Document document in documents.Values)
        globalCount += (document[word] != 0) ? 1 : 0;
      if (globalCount == 0)
        return 0;
    return Math.Log((double) (documents.Count / globalCount)) + 1d;
    }

    public static double Idf(string word, Corpus corpus)
    {
      if (corpus.idfs[word] != null)
        return (double) corpus.idfs[word]!;
      else
      {
        var idf = Idf_(word, corpus);
        corpus.idfs[word] = idf;
        return idf;
      }
    }

#endregion
  }
}
