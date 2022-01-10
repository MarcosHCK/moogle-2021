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
  [MimeType (MimeType = "text/plain")]
  public class PlainLoader : Loader
  {
#region Variables
    private static readonly Regex word_pattern = new Regex ("\\w+", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly int bufferSize = 1024;
    private static readonly int blockSize = 128;

#endregion

#region IEnumerable

    public override IEnumerator<(string Word, decimal Offset)> GetEnumerator()
    {
      var token = Task.Factory.CancellationToken;
      token.ThrowIfCancellationRequested();
      var cancellable = new GLib.Cancellable();
      token.Register(() => cancellable.Cancel());

      var stream__ = Source.Read(cancellable);
      var stream_ = new GLib.GioStream(stream__);
      var stream = new StreamReader(stream_, null, true, bufferSize, false);
      var builder = new StringBuilder();
      var block = new char[blockSize];
      var offset = (decimal) 0;
      int read;

      IEnumerable<string> EmitPartial (string partial)
      {
        var match =
        word_pattern.Match (partial);
        do
        {
          if (match.Success == true)
          {
            string word = match.Value;
            string lower = word.ToLower ();

            yield return word;
            if (word != lower)
              yield return lower;
          }
          else
          {
            break;
          }
        } while ((match = match.NextMatch ()) != null);

        yield return partial;
      }

      IEnumerable<(string,decimal)> EmitBlock (int read)
      {
        bool cr = false;
        char c;
        int i;

        for(i = 0; i < read; i++)
        {
          offset +=
          stream.CurrentEncoding.GetByteCount(block, i, 1);
          c = block[i];

          switch (c)
          {
          case '\r':
            cr = true;
            break;
          case '\n':
            cr = false;
            foreach (var word in EmitPartial (builder.ToString ()))
              yield return (word, offset);
            builder.Clear ();
            break;
          case '\x20':
            foreach (var word in EmitPartial (builder.ToString ()))
              yield return (word, offset);
            builder.Clear ();
            break;
          default:
            if (cr == true)
            {
              foreach (var word in EmitPartial (builder.ToString ()))
                yield return (word, offset);
              builder.Clear ();
            }
            builder.Append (c);
            break;
          }
        }
      }

      do
      {
        read =
        stream.Read (block, 0, blockSize);
        if (read > 0)
        {
          token.ThrowIfCancellationRequested();
          foreach (var tuple in EmitBlock (read))
            yield return tuple;
        }
        else
        {
          block[0] = '\n';
          token.ThrowIfCancellationRequested();
          foreach (var tuple in EmitBlock (read))
            yield return tuple;
        }
      }
      while (read != 0);
    }

#endregion

#region Constructors
    public PlainLoader(GLib.IFile source) : base(source) { }
#endregion
  }
}
