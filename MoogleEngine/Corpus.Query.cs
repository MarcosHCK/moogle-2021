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
using System.Text;

namespace Moogle.Engine
{
  public partial class Corpus
  {
    public partial class Query
    {
      private static Operator[]? operators;
      private static Regex? word_pattern;
      private static int[,]? levenshtein_matrix = null;
      public Dictionary<string, Word> Words { get; private set; }
      public string? OriginalQuery { get; private set; }

#region Where the works gets done

      public static int Levenshtein (string word1, string word2)
      {
        var matrix = levenshtein_matrix;
        var length1 = word1.Length + 1;
        var length2 = word2.Length + 1;
        int minimun = 0;
        int i, j;

        if (matrix == null || (
              matrix.GetLength (0) < length1
            || matrix.GetLength (1) < length2))
          {
            levenshtein_matrix = new int[length1, length2];
            matrix = levenshtein_matrix;
          }

        for (i = 1; i < length1; i++)
          matrix[i, 0] = i;
        for (i = 1; i < length2; i++)
          matrix[0, i] = i;
        matrix[0, 0] = 0;

        for (i = 1; i < length1; i++)
          for (j = 1; j < length2; j++)
          {
            if (word1[i - 1] == word2[j - 1])
            {
              matrix[i, j] = matrix[i - 1, j - 1];
            }
            else
            {
              minimun = Math.Min (matrix[i - 1, j], matrix[i, j - 1]);
              minimun = Math.Min (matrix[i - 1, j - 1], minimun) + 1;
              matrix[i, j] = minimun;
            }
          }

        minimun = matrix[length1 - 1, length2 - 1];
      return minimun;
      }

      private static string? ClosetWord (string target, Corpus.Document vector)
      {
        var length = target.Length;
        long closet = long.MaxValue;
        string? closer = null;

        foreach (var word in vector.Words.Keys)
        {
          var distance =
          Math.Abs (word.Length - length);
          if (distance < 3)
          {
            distance =
            Levenshtein (word, target);
            if (distance < closet)
            {
              closet = distance;
              closer = word;
            }
          }
        }
      return closer;
      }

      public static double Similarity (Corpus.Query query, Corpus.Document vector, Corpus corpus, Morph morph)
      { /* A = this, B = vector */
        /* similarity = ( A*B / ||A|| * ||B|| ) */
        double norm1 = 0; /* || A || */  /* SQRT( SUM( Ai^2  ) ) */
        double norm2 = 0; /* || B || */  /* SQRT( SUM( Bi^2  ) ) */
        double cross = 0; /*  A * B  */  /* SQRT( SUM( Ai*Bi ) ) */
        double tf1, tf2, idf, tfidf1, tfidf2;

        /* Calculate norm1, norm2 and cross for document words */
        foreach (var word_ in query.Words)
        {
          var word = word_.Key;
          tf1 = Corpus.Tf (word_.Value.Occurrences);
          tf2 = Corpus.Tf (word, vector);

          if (tf2 == 0)
          {
            Corpus.Word? store;
            if (corpus.Words.TryGetValue (word, out store))
            {
              morph.Alternative (query, word, store.Self);
              tf2 = Corpus.Tf (store, vector);
              idf = Corpus.Idf (store, corpus);
            }
            else
            {
              var closer =
              ClosetWord (word, vector);
              if (closer != null)
              {
                morph.Alternative (query, word, closer);
                tf2 = Corpus.Tf (closer, vector);
                idf = Corpus.Idf (closer, corpus);
              }
              else
              {
                idf = 0;
              }
            }
          }
          else
          {
            idf = Corpus.Idf (word, corpus);
          }

          tfidf1 = tf1 * idf;
          tfidf2 = tf2 * idf;

          norm1 += tfidf1 * tfidf1;
          cross += tfidf1 * tfidf2;
        }

        norm2 = vector.Norm;
        double norm1r = Math.Sqrt (norm1);
        double norm2r = Math.Sqrt (norm2);
        if (norm1r == 0 || norm2r == 0)
          return 0d;
      return cross / (norm1r * norm2r);
      }

      private string? GetSnippet (Corpus corpus, Document vector, GLib.IFile from)
      {
        var builder = new StringBuilder ();
        var havenot = new StringBuilder ();
        foreach (var word in this.Words.Keys)
        {
          var exists =
          vector.Words.ContainsKey (word);
          if (exists == true)
          {
            var store = corpus.Words[word];
            var offset = store.Locations[vector].Offsets[0];
            var snippet = corpus.GetSnippet(from, offset);
            var esc = GLib.Markup.EscapeText (snippet);
            builder.AppendFormat ("<span>{0}</span>", esc);
            builder.AppendLine ();
          }
          else
          {
            var esc = GLib.Markup.EscapeText (word);
            if (havenot.Length != 0)
              havenot.Append (' ');
            havenot.AppendFormat ("<s>{0}</s>", esc);
          }
        }

        var havenot_ = havenot.ToString ();
        builder.AppendLine (havenot_);
      return builder.ToString ();
      }

      public static (SearchItem[], string?) Perform (Corpus corpus, params Corpus.Query[] queries)
      {
        /* Items list */
        var items = new List<(Corpus.Query Query, Morph Morph, GLib.IFile Document, double Score)>();
        double max = double.MinValue;

        /* Calculate per-vector, similarity with corpus' documents */
        foreach (Corpus.Query query in queries)
        {
          foreach (var document in corpus.Documents)
          {
            var morph = new Morph (query.OriginalQuery!);
            Corpus.Document vector = document.Value;
            GLib.IFile key = document.Key;

            var score =
            Corpus.Query.Similarity (query, vector, corpus, morph);
            foreach (string word in query.Words.Keys)
            {
              var counter = query.Words[word];
              var filter = counter.Filter;
              if (filter != null)
                score = filter (query, corpus, vector, score);
            }

            items.Add ((query, morph, key, score));

            /* Take biggest score */
            if (score > max)
              max = score;
          }
        }

        if (0 >= max)
          return (new SearchItem[0], null);

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
        double bestmorphd = double.MinValue;
        Morph? bestmorph = null;

        /* create entries */
        foreach (var item in items)
        {
          if (item.Score > ceil)
          {
            var score = item.Score / max;
            var vector = corpus.Documents[item.Document];
            var snippet = item.Query.GetSnippet (corpus, vector, item.Document);
            if (snippet == null)
              snippet = "Can't load snippet for vector";
            array[i++] = new SearchItem (item.Document.ParsedName, snippet, score);

            if (score > bestmorphd)
            {
              bestmorphd = score;
              bestmorph = item.Morph;
            }
          }
        }

        /* Sort array */
        Array.Sort<SearchItem>(array);

        /* Extract best suggestion */
        string? suggestion = null;
        if (bestmorph != null)
          suggestion = bestmorph.Suggestion;
      return (array, suggestion);
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

#endregion

#region Constructors

      public class FallbackOperator : Operator
      {
        private static readonly Capture __default__ = new Capture ();

        public override string? BeginCapture (ref Capture? context, Match first, Match current)
        {
          context = __default__;
          return current.Value;
        }

        public override Filter? EndCapture (ref Capture? context) => null;
      }

      private Query ()
      {
        if (operators == null)
        {
          var list = Utils.GetImplementors (typeof(Operator));
          var operator_list = new List<Operator>();
          var pattern_married = new StringBuilder ();
          var pattern_single = new StringBuilder ();
          var pattern = new StringBuilder ();

          foreach (Type type in list)
            {
              if (type != typeof(FallbackOperator))
                {
                  var object_ = Activator.CreateInstance (type);
                  operator_list.Add ((Operator) object_!);
                }

              var attributes =
              type.GetCustomAttributes (typeof (Operator.GlyphAttribute), false);
              foreach (var attribute in attributes)
                {
                  var attr = (Operator.GlyphAttribute) attribute;
                  if (!attr.Single)
                    pattern_married.AppendFormat ("\\{0}", attr.Glyph);
                  else
                    pattern_single.AppendFormat ("\\{0}", attr.Glyph);
                }
            }

          operator_list.Add (new FallbackOperator());
          operators = new Operator[operator_list.Count];
          operator_list.CopyTo (0, operators, 0, operators.Length);

          if (operator_list.Count > 1)
            {
              var pttern =
              pattern_married.ToString ();
              pattern.Append ("[");
              pattern.Append (pttern);
              pattern.Append ("]*");
            }
            {
              var pttern =
              pattern_single.ToString ();
              pattern.Append ("[");
              pattern.Append (pttern);
              pattern.Append ("\\p{L}\\p{N}");
              pattern.Append ("]+");
            }

          var flags = RegexOptions.Compiled | RegexOptions.Singleline;
          word_pattern = new Regex (pattern.ToString (), flags);
        }

        OriginalQuery = null;
        Words = new Dictionary<string, Word> ();
      }

      public Query (string query) : this ()
      {
        Match match, first;
        Word? counter = null;

        OriginalQuery = query;
        match = word_pattern!.Match (query);
        first = match;
        
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
              if (capture!.emit)
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

                counter.Offsets.Add (match.Index);
              }

              filter =
              operator_!.EndCapture (ref capture);
              if (filter != null)
              {
                if (counter != null)
                {
                  counter.Filter = filter;
                }
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
