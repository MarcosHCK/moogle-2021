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
  public abstract class Document
  {
#region Variables

    public string Etag {get; set;}
    protected string InvalidEtag = "";
    public GLib.IFile Source {get; set;}
    protected Hashtable words = new Hashtable();
    protected decimal globalCount;

    protected class Counter
    {
      public decimal count = 1;
    }

#endregion

#region Abstracts

    public abstract void UdpdateTfImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null);

#endregion

#region Internal API

    protected void CalculateTf()
    {
      foreach (var word in words.Keys)
      {
        var counter = ((Counter)words[word]!);
        counter.count /= globalCount;
      }
    }

#endregion

#region API

    public void UdpdateTf(GLib.Cancellable? cancellable = null)
    {
      var info =
      Source.QueryInfo("etag::value", GLib.FileQueryInfoFlags.None, cancellable);
      if (info.Etag != Etag)
      {
        /* open file */
        var stream =
        Source.Read(cancellable);

        /* Perferm implementation specific update */
        UdpdateTfImplementation(stream, cancellable);

        /* Close stream and update etag */
        stream.Close(cancellable);
        Etag = info.Etag;
      }
    }

#endregion

#region Constructors

    public Document(GLib.IFile source)
    {
      this.Source = source;
      this.Etag = InvalidEtag;
    }

#endregion
  }
}
