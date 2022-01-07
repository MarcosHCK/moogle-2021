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
    private static readonly Regex word_pattern = new Regex("[\\^\\!\\*]*[\\w]+", RegexOptions.Compiled | RegexOptions.Singleline);
    private static GLib.IFile _default_ = (GLib.IFile) GLib.Object.GetObject((IntPtr) 0);
    private static QueryOperator[]? operators;

#endregion

#region Counter subclass

    private class QueryCounter : Counter
    {
      public QueryOperator.Filter? filter;
    }

#endregion

#region API

    protected override void UpdateImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null) => throw new NotImplementedException();
    protected override string SnippetImplementation(string word, GLib.InputStream stream, GLib.Cancellable? cancellable = null) => throw new NotImplementedException();

    public double Similarity (Document vector, Corpus corpus)
    { /* A = this, B = vector */
      /* similarity = ( A*B / ||A|| * ||B|| ) */
      double norm1 = 0; /* || A || */  /* SQRT( SUM( Ai^2  ) ) */
      double norm2 = 0; /* || B || */  /* SQRT( SUM( Bi^2  ) ) */
      double cross = 0; /*  A * B  */  /* SQRT( SUM( Ai*Bi ) ) */

      /* Calculate norm1, norm2 and cross for document words */
      foreach (string word in this)
      {
        double tf1 = Corpus.Tf(word, this);
        double tf2 = Corpus.Tf(word, vector);
        double idf = Corpus.Idf(word, corpus);
        double tfidf1 = tf1 * idf;
        double tfidf2 = tf2 * idf;

        norm1 += tfidf1 * tfidf1;
        norm2 += tfidf2 * tfidf2;
        cross += tfidf1 * tfidf2;
      }

      /* Calculate norm1, norm2 and cross for query words */
      foreach (string word in vector)
      {
        /*
         * Filter out the words' components we already
         * calculated earlier 
         *
         */

        if (this[word] == 0)
        {
          double tf = Corpus.Tf(word, vector);
          double idf = Corpus.Idf(word, corpus);
          double tfidf = tf * idf;
          norm2 += tfidf * tfidf;
        }
      }

      double norm1r = Math.Sqrt(norm1);
      double norm2r = Math.Sqrt(norm2);
      if(norm1r == 0 || norm2r == 0)
        return 0d;
    return cross / (norm1r * norm2r);
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
      foreach (QueryDocument query in queries)
      {
        foreach(Document document in corpus)
        {
          var score = query.Similarity(document, corpus);
          {
            foreach (string word in query)
            {
              var counter = (QueryCounter?) query.words[word];
              if (counter != null)
              {
                var filter = counter.filter;
                if (filter != null)
                  score = filter(query, document, score);
              }
            }
          }

          var title = document.ToString()!;
          items.Add((title, score, query, document));

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

      double ceil = 0.3d * max;
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

#region Operators

    private static (string?, QueryOperator?) BeginCapture(ref QueryOperator.Capture? context, Match first, Match current)
    {
      foreach (var operator_ in operators!)
      {
        QueryOperator.Capture? context_ = null;
        string? value = null;

        value =
        operator_.BeginCapture(ref context_, first, current);
        if (value != null)
        {
          context = context_;
          return (value, operator_);
        }
      }
    return (null,null);
    }

    private static QueryOperator.Filter? EndCapture(ref QueryOperator.Capture? context)
    {
      foreach (var operator_ in operators!)
      {
        QueryOperator.Capture? context_ = context;
        QueryOperator.Filter? value = null;

        value =
        operator_.EndCapture(ref context_);
        if (value != null)
        {
          return value;
        }
      }
    return null;
    }

#endregion

#region Constructors

    public class FallbackOperator : QueryOperator
    {
      public override string? BeginCapture(ref Capture? context, Match first, Match current) => current.Value;
      public override Filter? EndCapture(ref Capture? context) => null;
    }

    private QueryDocument() : base(_default_)
    {
      if (operators == null)
      {
        var list = Utils.GetImplementors(typeof(QueryOperator));
        var operator_list = new List<QueryOperator>();

        foreach (Type type in list)
        if(type != typeof(FallbackOperator))
          {
            var object_ = Activator.CreateInstance(type);
            operator_list.Add((QueryOperator) object_!);
          }

        operator_list.Add(new FallbackOperator());

        operators = new QueryOperator[operator_list.Count];
        operator_list.CopyTo(0, operators, 0, operators.Length);
      }
    }

    public QueryDocument(string query) : this()
    {
      var match =
      word_pattern.Match(query);
      var first = match;
      
      do
      {
        if (match.Success)
        {
          QueryOperator.Capture? capture = null;
          QueryOperator.Filter? filter = null;
          QueryOperator? operator_ = null;
          QueryCounter? counter = null;
          string? word;

          var state = 
          BeginCapture (ref capture, first, match);
          operator_ = state.Item2;
          word = state.Item1;

          if (word != null)
          {
            if (words.ContainsKey(word))
            {
              counter = (QueryCounter) words[word]!;
              counter.count++;
              globalCount++;
            }
            else
            {
              counter = new QueryCounter();
              words[word] = counter;
              globalCount++;
            }

            filter =
            operator_!.EndCapture (ref capture);
            if (filter != null)
            {
              counter.filter = filter;
            }
          }
        }
        else
        {
          break;
        }
      }
      while ((match = match.NextMatch()) != null);
    }

#endregion
  }
}
