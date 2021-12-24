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
using System.Threading;

namespace Moogle.Engine
{
  public class SearchEngine
  {
#region Variables
    public string Source {get; private set;}
    private Corpus corpus = new Corpus();

    [System.Serializable]
    public sealed class SearchEngineException : System.Exception
    {
      public SearchEngineException() { }
      public SearchEngineException(string message) : base(message) { }
      public SearchEngineException(string message, System.Exception inner) : base(message, inner) { }
      private SearchEngineException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

#endregion

#region Workers

    private void ScanDirectory(GLib.IFile directory, GLib.Cancellable? cancellable = null)
    {
      var enumerator =
      directory.EnumerateChildren("standard::name,standard::content-type", GLib.FileQueryInfoFlags.None, cancellable);
      foreach (var info in enumerator)
      {
        switch(info.FileType)
        {
        case GLib.FileType.Directory:
          ScanDirectory(directory.GetChild(info.Name), cancellable);
          break;
        case GLib.FileType.Regular:
          corpus.Add(directory.GetChild(info.Name), info, cancellable);
          break;
        }

        if (cancellable != null
          && cancellable.IsCancelled)
          return;
      }
    }

    private void UpdateDocumentList(GLib.Cancellable? cancellable = null)
    {
      /* Scan source directory */
#region Directory loading
      GLib.IFile? file = null;

      file = GLib.FileFactory.NewForPath(Source);
      if (file == null)
      {
        throw new Exception("I don't known, maybe some 'c#'s gdb' is needed");
      }

      /* Scan it! */
      ScanDirectory(file, cancellable);
      if (cancellable != null
          && cancellable.IsCancelled)
        return;
#endregion
    }

#endregion

#region API

    public SearchResult Query(string query)
    {
      /* Prepare cncellable object */
      var token = Task.Factory.CancellationToken;
      var cancellable = new GLib.Cancellable();
      token.Register(() => cancellable.Cancel());

      /* update document list */
      UpdateDocumentList(cancellable);
      token.ThrowIfCancellationRequested();
      /* reload documents which had been modified */
      corpus.Update();
      token.ThrowIfCancellationRequested();

      /* Perform a word search */
      var list = corpus.SearchItems(query);
      var items = new SearchItem[list.Count];
      int i = 0;
      foreach (var item in list)
        items[i++] = item;
      return new SearchResult(items, query);
    }

#endregion

#region Constructors

    public SearchEngine() : this(".") {}
    public SearchEngine(string source) => this.Source = source;
#endregion
  }
}
