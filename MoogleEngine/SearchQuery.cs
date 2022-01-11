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
using System.Collections;

namespace Moogle.Engine
{
  public class SearchQuery : System.Object, IEnumerable<string>
  {
#region Variables
    private static readonly Regex word_pattern = new Regex("[\\^\\!\\*]*[\\w]+", RegexOptions.Compiled | RegexOptions.Singleline);
    private static QueryOperator[]? operators;
    public Dictionary<string, Word> Words {get; private set;}

#endregion

#region Word records

    public struct Word
    {
      public string word;
      public decimal count;
      public QueryOperator.Filter filter;
    }

#endregion

#region Calculus functions

    public static double Similarity (SearchQuery query, Corpus corpus, Corpus.Document vector)
    { /* A = this, B = vector */
      /* similarity = ( A*B / ||A|| * ||B|| ) */
      double norm1 = 0; /* || A || */  /* SQRT( SUM( Ai^2  ) ) */
      double norm2 = 0; /* || B || */  /* SQRT( SUM( Bi^2  ) ) */
      double cross = 0; /*  A * B  */  /* SQRT( SUM( Ai*Bi ) ) */

      /* Calculate norm1, norm2 and cross for document words */
      foreach (string word in query)
      {
        double tf1 = query.Words.ContainsKey (word) ? 1d : 0d;
        double tf2 = Corpus.Tf (word, vector);
        double idf = Corpus.Idf (word, corpus);
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

        if (query.Words.ContainsKey (word))
        {
          double tf = Corpus.Tf (word, vector);
          double idf = Corpus.Idf (word, corpus);
          double tfidf = tf * idf;
          norm2 += tfidf * tfidf;
        }
      }

      double norm1r = Math.Sqrt (norm1);
      double norm2r = Math.Sqrt (norm2);
      if (norm1r == 0 || norm2r == 0)
        return 0d;
    return cross / (norm1r * norm2r);
    }

    public static SearchItem[] Perform(Corpus corpus, params SearchQuery[] queries)
    {
      /* Items list */
      var items = new List<(SearchQuery SearchQuery, GLib.IFile Document, double Score)>();
      double max = double.MinValue;

      /* Calculate per-vector, similarity with corpus' documents */
      foreach (SearchQuery query in queries)
      {
        foreach (var document in corpus.Documents)
        {
          Corpus.Document vector = document.Value;
          GLib.IFile key = document.Key;

          var score =
          SearchQuery.Similarity (query, corpus, vector);
          foreach (string word in query)
          {
            var counter = query.Words[word];
            var filter = counter.filter;
            if (filter != null)
              score = filter(query, corpus, vector, score);
          }

          items.Add((query, key, score));

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
        if (item.Score > ceil)
          elements++;
      }

      array = new SearchItem[elements];

      /* create entries */
      foreach (var item in items)
      {
        if (item.Score > ceil)
        {
          var vector = corpus.Documents[item.Document];
          //var snippet = vector.GetSnippet ();
          var snippet = $"score {0}";
          if(snippet == null)
            snippet = "Can't load snippet for vector";
          array[i++] = new SearchItem(item.Document.ParsedName, snippet, item.Score / max);
        }
      }

      /* Sort array */
      Array.Sort<SearchItem>(array);
    return array;
    }

#endregion

#region IEnumerable

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<string> GetEnumerator() => Words.Keys.GetEnumerator();

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

    private SearchQuery()
    {
      this.Words = new Dictionary<string, Word>();

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

    public SearchQuery(string query) : this ()
    {
      var match =
      word_pattern.Match(query);
      var first = match;
      Word counter;
      
      do
      {
        if (match.Success)
        {
          QueryOperator.Capture? capture = null;
          QueryOperator.Filter? filter = null;
          QueryOperator? operator_ = null;
          string? word;

          var state = 
          BeginCapture (ref capture, first, match);
          operator_ = state.Item2;
          word = state.Item1;

          if (word != null)
          {
            if (Words.ContainsKey(word))
            {
              counter = Words[word]!;
              counter.count++;
            }
            else
            {
              Words.Add (word, new Word());
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
