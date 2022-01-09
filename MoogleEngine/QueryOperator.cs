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
  public abstract class QueryOperator
  {
    public class Capture
    {
      public string instance = "";
    }

    public delegate double Filter(Document query, Document vector, double score);

    public abstract string? BeginCapture(ref Capture? context, Match first, Match current);
    public abstract Filter? EndCapture(ref Capture? context);
  }

#region Operators

  namespace Moogle.Engine.Operators
  {
    public class MustExistsOperator : QueryOperator
    {
      public override string? BeginCapture(ref Capture? context, Match first, Match current)
      {
        context = null;
        string word = current.Value;

        if (word[0] == '^')
        {
          word = word.Substring(1);

          Capture
          context_ = new Capture();
          context_.instance = word;
          context = context_;
          return word;
        }
      return null;
      }

      public override Filter? EndCapture(ref Capture? context)
      {
        if (context != null)
        {
          string word = ((Capture) context!).instance;
          return (Document query, Document vector, double score) =>
          {
            if (vector[word] != 0)
              return score;
            else
              return 0d;
          };
        }
      return null;
      }
    }

    public class MustNotExistsOperator : QueryOperator
    {
      public override string? BeginCapture(ref Capture? context, Match first, Match current)
      {
        context = null;
        string word = current.Value;

        if (word[0] == '!')
        {
          word = word.Substring(1);

          Capture
          context_ = new Capture();
          context_.instance = word;
          context = context_;
          return word;
        }
        return null;
      }

      public override Filter? EndCapture(ref Capture? context)
      {
        if (context != null)
        {
          string word = ((Capture)context!).instance;
          return (Document query, Document vector, double score) =>
          {
            if (vector[word] == 0)
              return score;
            else
              return 0d;
          };
        }
        return null;
      }
    }

    public class ImportanceOperator : QueryOperator
    {
      private class MoreCapture : QueryOperator.Capture
      {
        public int importance;
      }

      public override string? BeginCapture(ref Capture? context, Match first, Match current)
      {
        context = null;
        string word = current.Value;

        if (word[0] == '*')
        {
          int i, exp = 1;
          for(i = 1; i < word.Length && word[i] == '*'; i++)
            exp++;
          word = word.Substring(exp);
          Console.WriteLine($"word {word}");

          var
          context_ = new MoreCapture();
          context_.instance = word;
          context_.importance = exp;
          context = context_;
          return word;
        }
        return null;
      }

      public override Filter? EndCapture(ref Capture? context)
      {
        throw new NotImplementedException();
      }
    }

    public class ProximityOperator : QueryOperator
    {
      private class MoreCapture : Capture
      {
        public string other = "";
      }

      [System.Serializable]
      public class ProximityOperatorException : System.Exception
      {
        public ProximityOperatorException() { }
        public ProximityOperatorException(string message) : base(message) { }
        public ProximityOperatorException(string message, System.Exception inner) : base(message, inner) { }
        protected ProximityOperatorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
      }

      public override string? BeginCapture(ref Capture? context, Match first, Match current)
      {
        context = null;
        Match? match, last = null;
        string word = current.Value;
        string? other = null;

        if (word[0] == '~')
        {
          if (current == first)
            throw new ProximityOperatorException($"Word '{word}' hasn't preceding one");

          word = word.Substring(1);
          for (match = first;
               match.Success;
               last = match,
               match = match.NextMatch())
          {
            if (match == current)
            {
              other = last!.Value;
              break;
            }
          }

          if (other == null)
            throw new ProximityOperatorException($"Error retreiving preciding word for '{word}'");

          var
          context_ = new MoreCapture();
          context_.instance = word;
          context_.other = other;
          context = context_;
          return word;
        }
      return null;
      }

      public override Filter? EndCapture(ref Capture? context)
      {
        if (context == null)
          return null;
        if (context.GetType() != typeof(MoreCapture))
          throw new ArgumentException($"Invalid capture context type '{context.GetType().Name}'");
        return (Document query, Document vector, double score) =>
        {
          throw new NotImplementedException();
        };
      }
    }
  }

#endregion
}
