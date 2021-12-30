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
  public class PlainDocument : Document
  {
#region Variables

    private static readonly Regex word_pattern = new Regex("\\w+", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly int bufferSize = 1024;
    private static readonly int blockSize = 64;
    private StringBuilder builder = new StringBuilder();
    private char[] buffer = new char[blockSize];

    private long position;
    private int offset;

    private delegate bool PartialWorker (string word);

#endregion

#region Overrides

    private bool EmitPartial(PartialWorker worker, string partial)
    {
      var match =
      word_pattern.Match(partial);
      if (match.Success)
      {
        do
        {
          if (match.Success)
          {
            string word = match.Value.ToLower();
            bool stop = worker(word);

            if (stop == true)
            {
              return stop;
            }
          }
          else
          {
            break;
          }
        }
        while ((match = match.NextMatch()) != null);
      }
    return false;
    }

    private bool EmitBlock(PartialWorker worker, char[] block, int read)
    {
      bool stop  = false;
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
            stop = EmitPartial(worker, builder.ToString());
            builder.Clear();
            if (stop == true) return stop;
            break;
          case '\x20':
            EmitPartial(worker, builder.ToString());
            builder.Clear();
            if (stop == true) return stop;
            break;
          default:
            if (cr == true)
            {
              cr = false;
              EmitPartial(worker, builder.ToString());
              builder.Clear();
              if (stop == true) return stop;
            }

            builder.Append(c);
            break;
        }
      }
    return stop;
    }

    private void ProcessStream(PartialWorker worker, StreamReader reader, GLib.Cancellable? cancellable = null)
    {
      builder.Clear();
      bool stop = false;
      int read;

      do
      {
        position =
        reader.BaseStream.Position;
        offset = 0;

        read =
        reader.ReadBlock(buffer, 0, blockSize);
        if (read > 0)
        {
          stop =
          EmitBlock(worker, buffer, read);
          if (cancellable != null
            && cancellable.IsCancelled)
            return;
          if (stop == false)
            return;
        }
        else
        {
          buffer[0] = '\n';
          stop =
          EmitBlock(worker, buffer, 1);
          if (stop == false)
            return;
          break;
        }
      }
      while (true);
    }

    protected override void UpdateImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null)
    {
      var wrapper = new GLib.GioStream(stream);
      var reader = new StreamReader(wrapper, null, true, bufferSize, true);

      globalCount = 0;
      words.Clear();

      ProcessStream((word) => {
        Counter counter;

        /* append word to hash table */
        if (words.ContainsKey(word))
        {
          counter = (Counter)words[word]!;
          counter.count++;
        }
        else
        {
          counter = new Counter();
          words[word] = counter;
        }

        /* append location */
        var loc = new Location();
        loc.index = globalCount;
        loc.position = position;
        loc.offset = offset;
        counter.locations.Add(loc);

        /* Counters */
        globalCount++;
        offset++;
      return false;
      }, reader, cancellable);
    }

    protected override string SnippetImplementation(string word, GLib.InputStream stream, GLib.Cancellable? cancellable = null)
    {
      var wrapper = new GLib.GioStream(stream);
      var reader = new StreamReader(wrapper, null, true, bufferSize, true);
      Location loc = ((Counter) words[word]!).locations[0];

      /*
       * Seek to blocks before
       * our word's position
       * (should be enough to
       * catch some words)
       *
       */

      const int blocksz = 33;
      const int blocks = 4;
      double size  = (double) word.Length;
      double clampt = Math.Log10(size + 1d) + 1d;
      long offset = ((long) clampt) - (blocksz * (blocks / 2));
      if(loc.position + offset < 0)
        loc.position = -offset;
      wrapper.Seek(loc.position + offset, SeekOrigin.Begin);

      var array = new char[blocksz * blocks];
      reader.ReadBlock(array, 0, array.Length);
    return new string(array);
    }

#endregion

#region Constructors

    public PlainDocument(GLib.IFile source) : base(source) { }

#endregion
  }
}
