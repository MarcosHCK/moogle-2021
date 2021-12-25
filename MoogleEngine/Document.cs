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
  public abstract class Document : IEnumerable, ICollection
  {
#region Variables

    private string Etag {get; set;}
    private GLib.IFile Source {get; set;}
    protected decimal globalCount = 0;
    protected Hashtable words;

    protected class Counter
    {
      public decimal count = 1;
    }

    public decimal this[string word] {
      get {
        object? object_ = words[word];
        if (object_ != null)
          return ((Counter) object_).count;
        else
          return 0;
      }}

#endregion

#region Abstracts

    public abstract void UpdateImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null);

#endregion

#region API

    public void Udpdate(GLib.Cancellable? cancellable = null)
    {
      var info =
      Source.QueryInfo("etag::value", GLib.FileQueryInfoFlags.None, cancellable);
      if (info.Etag != Etag)
      {
        /* open file */
        var stream =
        Source.Read(cancellable);

        /* Perferm implementation specific update */
        UpdateImplementation(stream, cancellable);

        /* Close stream and update etag */
        stream.Close(cancellable);
        Etag = info.Etag;
      }
    }

#endregion

#region ICollection

    public int Count {
      get {
        return words.Keys.Count;
      }}
    public bool IsSynchronized {
      get {
        return false;
      }}
    public object SyncRoot {
      get {
        return (object) this;
      }}

    public void CopyTo(Array array, int index) => words.Keys.CopyTo(array, index);

#endregion

#region IEnumerable

    public IEnumerator GetEnumerator() => words.Keys.GetEnumerator();

#endregion

#region System.Object

    public override string? ToString() => this.Source.Basename;

#endregion

#region Constructors

    public Document(GLib.IFile source)
    {
      this.words = new Hashtable();
      this.Source = source;
      this.Etag = "0:0";
    }

#endregion
  }
}
