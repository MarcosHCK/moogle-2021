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
  public class Corpus
  {
#region Variables
    public Hashtable documents = new Hashtable();
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
          ((Document) entry.Value!).Udpdate(cancellable);

          if (cancellable != null
            && cancellable.IsCancelled)
            return;
        }
        catch(GLib.GException e)
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

    public double TfIdf(string word, Document document) => Tf(word, document) * Idf(word, this);

    public List<SearchItem> SearchItems(string word)
    {
      var idf = Idf(word, this);
      var list = new List<SearchItem>();
      foreach (Document document in documents.Values)
      {
        var tf = Tf(word, document);
        if (tf > 0)
        {
          var score = tf * idf;
          var snippet = $"{word}: {tf * idf}";
          list.Add(new SearchItem(document.Source.ParsedName, snippet, score));
        }
      }

      list.Sort();
    return list;
    }

#endregion

#region TF-IDF functions

    public static double Tf(string word, Document document)
    {
      decimal count = document.GetWordCount(word);
      if (count == 0)
        return 0;
      else
      {
        return Math.Log((double) (count)) + 1d;
      }
    }

    public static double Idf(string word, Corpus corpus)
    {
      /* Count wor occurrencies */
      decimal globalCount = 0;
      var documents = corpus.documents;

      foreach (Document document in documents.Values)
        globalCount += (document.GetWordCount(word) != 0) ? 1 : 0;
      if (globalCount == 0)
        return 0;
    return Math.Log((double) (documents.Count / globalCount));
    }

#endregion
  }
}
