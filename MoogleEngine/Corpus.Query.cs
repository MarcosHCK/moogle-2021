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
      private static readonly Regex word_pattern = new Regex("[\\^\\!\\*]*[\\w]+", RegexOptions.Compiled | RegexOptions.Singleline);
      private static Operator[]? operators;
      public Dictionary<string, Word> Words { get; private set; }

#region Where the works gets done

      public static double Similarity (Corpus.Query query, Corpus corpus, Corpus.Document vector)
      { /* A = this, B = vector */
        /* similarity = ( A*B / ||A|| * ||B|| ) */
        double norm1 = 0; /* || A || */  /* SQRT( SUM( Ai^2  ) ) */
        double norm2 = 0; /* || B || */  /* SQRT( SUM( Bi^2  ) ) */
        double cross = 0; /*  A * B  */  /* SQRT( SUM( Ai*Bi ) ) */
        double tf1, tf2, idf, tfidf1, tfidf2;

        /* Calculate norm1, norm2 and cross for document words */
        foreach (string word in query.Words.Keys)
        {
          tf1 = 1d;
          tf2 = Corpus.Tf (word, vector);
          if (tf2 == 0)
          {
            Corpus.Word? store;
            if (corpus.Words.TryGetValue (word, out store))
            {
              tf2 = Corpus.Tf (store, vector);
            }
          }

          idf = Corpus.Idf (word, corpus);
          tfidf1 = tf1 * idf;
          tfidf2 = tf2 * idf;

          norm1 += tfidf1 * tfidf1;
          norm2 += tfidf2 * tfidf2;
          cross += tfidf1 * tfidf2;
        }

        /* Calculate norm1, norm2 and cross for query words */
        foreach (string word in vector.Words.Keys)
        {
          /*
          * Filter out the words' components we already
          * calculated earlier 
          *
          */

          if (query.Words.ContainsKey (word) == false)
          {
            tf2 = Corpus.Tf (word, vector);
            idf = Corpus.Idf (word, corpus);
            tfidf2 = tf2 * idf;
            norm2 += tfidf2 * tfidf2;
          }
        }

        double norm1r = Math.Sqrt (norm1);
        double norm2r = Math.Sqrt (norm2);
        if (norm1r == 0 || norm2r == 0)
          return 0d;
      return cross / (norm1r * norm2r);
      }

      private string? GetSnippet (Corpus corpus, Document vector, GLib.IFile from)
      {
        string? word = null;
        double score = double.MinValue;

        foreach (var word_ in this.Words.Keys)
        {
          var exists =
          corpus.Words.ContainsKey (word_);
          if (exists == true)
          {
            var ctx = corpus.Words[word_];
            var score_ = Corpus.Idf (word_, corpus);

            exists =
            ctx.Locations.ContainsKey (vector);
            if (exists == true)
            {
              if (score_ > score)
              {
                score = score_;
                word = word_;
              }
            }
          }
        }

        if (word != null)
        {
          var store = corpus.Words[word];
          var offset = store.Locations[vector].Offsets[0];
          return corpus.GetSnippet (from, offset);
        }
      return null;
      }

      public static SearchItem[] Perform(Corpus corpus, params Corpus.Query[] queries)
      {
        /* Items list */
        var items = new List<(Corpus.Query Query, GLib.IFile Document, double Score)>();
        double max = double.MinValue;

        /* Calculate per-vector, similarity with corpus' documents */
        foreach (Corpus.Query query in queries)
        {
          foreach (var document in corpus.Documents)
          {
            Corpus.Document vector = document.Value;
            GLib.IFile key = document.Key;

            var score =
            Corpus.Query.Similarity (query, corpus, vector);
            foreach (string word in query.Words.Keys)
            {
              var counter = query.Words[word];
              var filter = counter.Filter;
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
            var snippet = item.Query.GetSnippet (corpus, vector, item.Document);
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

#region Operators

      private static (string?, Operator?) BeginCapture (ref Operator.Capture? context, Match first, Match current)
      {
        foreach (var operator_ in operators!)
        {
          Operator.Capture? context_ = null;
          string? value = null;

          value =
          operator_.BeginCapture (ref context_, first, current);
          if (value != null)
          {
            context = context_;
            return (value, operator_);
          }
        }
      return (null,null);
      }

      private static Operator.Filter? EndCapture (ref Operator.Capture? context)
      {
        foreach (var operator_ in operators!)
        {
          Operator.Capture? context_ = context;
          Operator.Filter? value = null;

          value =
          operator_.EndCapture (ref context_);
          if (value != null)
          {
            return value;
          }
        }
      return null;
      }

#endregion

#region Constructors

      public class FallbackOperator : Operator
      {
        public override string? BeginCapture (ref Capture? context, Match first, Match current) => current.Value;
        public override Filter? EndCapture (ref Capture? context) => null;
      }

      public Query ()
      {
        Words = new Dictionary<string, Word> ();
        if (operators == null)
        {
          var list = Utils.GetImplementors (typeof(Operator));
          var operator_list = new List<Operator>();

          foreach (Type type in list)
          if(type != typeof(FallbackOperator))
            {
              var object_ = Activator.CreateInstance (type);
              operator_list.Add ((Operator) object_!);
            }

          operator_list.Add (new FallbackOperator());

          operators = new Operator[operator_list.Count];
          operator_list.CopyTo (0, operators, 0, operators.Length);
        }
      }

      public Query (string query) : this ()
      {
        var match =
        word_pattern.Match (query);
        var first = match;
        Word counter;
        
        do
        {
          if (match.Success)
          {
            Operator.Capture? capture = null;
            Operator.Filter? filter = null;
            Operator? operator_ = null;
            string? word;

            var state = 
            BeginCapture (ref capture, first, match);
            operator_ = state.Item2;
            word = state.Item1;

            if (word != null)
            {
              if (Words.ContainsKey (word))
              {
                counter = Words[word]!;
                counter.Occurrences++;
              }
              else
              {
                Words.Add (word, new Word());
                counter = Words[word];
              }

              filter =
              operator_!.EndCapture (ref capture);
              if (filter != null)
              {
                counter.Filter = filter;
              }
            }
          }
          else
          {
            break;
          }
        }
        while ((match = match.NextMatch ()) != null);
      }

#endregion
    }
  }
}
