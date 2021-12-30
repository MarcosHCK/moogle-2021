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
  public class QueryDocument : Document
  {
#region Variables
    private static readonly Regex word_pattern = new Regex("[\\w]+", RegexOptions.Compiled | RegexOptions.Singleline);
    private static GLib.IFile _default_ = (GLib.IFile)GLib.Object.GetObject((IntPtr) 0);
    private Corpus query = new Corpus();

#endregion

#region API

    private double tfidf(string word)
    {
      var count = this[word];
      if (count > 0)
        return Math.Log((double) count) + 1d;
      else
        return 0d;
    }

    protected override void UpdateImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null) => throw new NotImplementedException();
    protected override string SnippetImplementation(string word, GLib.InputStream stream, GLib.Cancellable? cancellable = null) => throw new NotImplementedException();

    public double Similarity (Document vector, Corpus corpus)
    {
      /* similarity = ( A*B / ||A|| * ||B|| ) */
      decimal norm1 = 0; /* || A || */  /* SQRT( SUM( Ai^2  ) ) */
      decimal norm2 = 0; /* || B || */  /* SQRT( SUM( Bi^2  ) ) */
      decimal cross = 0; /*  A * B  */  /* SQRT( SUM( Ai*Bi ) ) */

      /* Calculate norm1, norm2 and cross for document words */
      foreach (string word in vector)
      {
        decimal tf = (decimal) Corpus.Tf(word, vector);
        decimal idf = (decimal) Corpus.Idf(word, corpus);
        decimal tfidf1 = (decimal) this.tfidf(word);
        decimal tfidf2 = tf * idf;

        norm1 += tfidf1 * tfidf1;
        norm2 += tfidf2 * tfidf2;
        cross += tfidf1 * tfidf2;
      }

      /* Calculate norm1, norm2 and cross for query words */
      foreach (string word in this)
      {
        /*
         * Filter out the words' components we already
         * calculated earlier 
         *
         */

        if (vector[word] == 0)
        {
          norm1 += (decimal) this.tfidf(word);
        }
      }

      double norm1r = Math.Sqrt((double) norm1);
      double norm2r = Math.Sqrt((double) norm2);
      double crossd = (double) cross;
    return crossd / (norm1r * norm2r);
    }

    public string? GetSnippet (Document document)
    {
      foreach (string word in words.Keys)
      {
        if (document[word] > 0)
        {
          return document.Snippet(word);
        }
      }
    return null;
    }

    public static SearchItem[] Perform(Corpus corpus, params QueryDocument[] queries)
    {
      /* Items list */
      var items = new List<(string title, double score, QueryDocument vector, Document document)>();
      double max = -1d;

      /* Calculate per-vector, similarity with corpus' documents */
      foreach (QueryDocument vector in queries)
      {
        foreach(Document document in corpus)
        {
          var score = vector.Similarity(document, corpus);
          var title = document.ToString()!;
          items.Add((title, score, vector, document));

          /* Take biggest score */
          if (score > max)
            max = score;
        }
      }

      if (0 >= max)
      {
        return new SearchItem[0];
      }

      /*
       * Copy to output array
       * and normalize it
       *
       */

      double ceil = 0.2d * max;
      int elements = 0, i = 0;
      SearchItem[] array;

      /* Count usable entries */
      foreach (var item in items)
      {
        if (item.score > ceil)
          elements++;
      }

      array = new SearchItem[elements];

      /* create entries */
      foreach (var item in items)
      {
        if (item.score > ceil)
        {
          var snippet = item.vector.GetSnippet(item.document);
          if(snippet == null)
            snippet = "Can't load snippet for vector";
          array[i++] = new SearchItem(item.title, snippet, item.score / max);
        }
      }

      /* Sort array */
      Array.Sort<SearchItem>(array);
    return array;
    }

#endregion

#region Constructors

    public QueryDocument(string query) : base(_default_)
    {
      var match =
      word_pattern.Match(query);
      if (match.Success)
      {
        do
        {
          if (match.Success)
          {
            var word = match.Value.ToLower();
            if (words.ContainsKey(word))
              ((Counter) words[word]!).count++;
            else
              words[word] = new Counter();
            globalCount++;
          }
          else
          {
            break;
          }
        }
        while ((match = match.NextMatch()) != null);
      }
    }

#endregion
  }
}
