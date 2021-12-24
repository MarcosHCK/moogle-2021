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
  [DocumentLoader.MimeType (MimeType = "text/plain")]
  public class PlainLoader : DocumentLoader
  {
    protected class PlainDocument : Document
    {
#region Variables

      private static Regex word_pattern = new Regex("\\w+", RegexOptions.Compiled | RegexOptions.Singleline);
      private static int bufferSize = 1024;
      private StringBuilder builder = new StringBuilder();
      private char[] buffer = new char[64];

#endregion

#region Document loading

      private void EmitPartial(string partial)
      {
        lock (word_pattern)
        {
          var match =
          word_pattern.Match(partial);
          if (match.Success)
          {
            do
            {
              if (match.Success)
              {
                var word = match.Value;
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
      }

      private void EmitBlock(char[] block, int read)
      {
        bool cr = false;
        for (int i = 0; i < read; i++)
        {
          var c = block[i];
          switch (c)
          {
            case '\r':
              cr = true;
              break;
            case '\n':
              cr = false;
              EmitPartial(builder.ToString());
              builder.Clear();
              break;
            case '\x20':
              EmitPartial(builder.ToString());
              builder.Clear();
              break;
            default:
              if (cr == true)
              {
                EmitPartial(builder.ToString());
                builder.Clear();
                cr = false;
              }

              builder.Append(c);
              break;
          }
        }
      }

      public override void UdpdateImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null)
      {
        var reader = new StreamReader(new GLib.GioStream(stream), null, true, bufferSize, true);
        globalCount = 0;
        builder.Clear();
        words.Clear();
        int read;

        do
        {
          read =
          reader.ReadBlock(buffer, 0, buffer.Length);
          if (read > 0)
          {
            EmitBlock(buffer, read);
            if (cancellable != null
              && cancellable.IsCancelled)
              return;
          }
          else
          {
            buffer[0] = '\n';
            EmitBlock(buffer, 1);
            break;
          }
        }
        while (true);
      }

#endregion

#region Constructors

      public PlainDocument(GLib.IFile source) : base(source) {}

#endregion
    }

    protected override Document LoadImplementation(GLib.IFile file, string MimeType, GLib.Cancellable? cancellable = null)
    {
      return new PlainDocument(file);
    }
  }
}
