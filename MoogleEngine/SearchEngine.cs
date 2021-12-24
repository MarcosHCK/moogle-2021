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
using System.Threading;

namespace Moogle.Engine
{
  public class SearchEngine
  {
    public string Source {get; private set;}
    public Hashtable documents = new Hashtable();

    /*
     * Nested types
     *
     */

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

    public class DocumentEntry
    {
      public Document? document = null;
      private bool Looked = false;

      public void Mark() => this.Looked = true;
      public void Unmark() => this.Looked = false;
      public bool IsMarked() => this.Looked == true;
    }

    /*
     * Workers
     *
     */

    private void UpdateFile(GLib.IFile file, GLib.FileInfo info, GLib.Cancellable? cancellable = null)
    {
      var entry = (DocumentEntry?) documents[file.ParsedName];
      if (entry == null)
      {
        var document = DocumentLoader.Load(file, info.ContentType, cancellable);
        if (document != null)
        {
          var entry_ = new DocumentEntry();
          entry_.document = document;
          documents.Add(file.ParsedName, entry_);
          entry_.Mark();
        }

        if (cancellable != null
          && cancellable.IsCancelled)
          return;
      }
      else
      {
        ((DocumentEntry) entry).Mark();
      }
    }

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
          UpdateFile(directory.GetChild(info.Name), info, cancellable);
          break;
        }

        if (cancellable != null
          && cancellable.IsCancelled)
          return;
      }
    }

    private void UpdateDocumentList(GLib.Cancellable? cancellable = null)
    {
      /* Unmark all documents */
      foreach (DocumentEntry doc in documents.Values)
      {
        doc.Unmark();
      }

      /* Load main source directory */
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

      /* If document has wasn't marked means it was deleted */
      do
      {
        string? remove = null;
        foreach (DictionaryEntry entry in documents)
        {
          var doc = (DocumentEntry) entry.Value!;
          if (doc.IsMarked() == false)
          {
            remove = (string) entry.Key;
            break;
          }
        }

        if (remove != null)
          documents.Remove(remove);
        else
          break;
      }
      while (true);
    }

    private void UpdateDocumentTf(GLib.Cancellable? cancellable = null)
    {
      foreach (DocumentEntry doc in documents.Values)
      {
        doc.document!.UdpdateTf(cancellable);
        if (cancellable != null
          && cancellable.IsCancelled)
          return;
      }
    }

    /*
     * API
     *
     */

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
      UpdateDocumentTf(cancellable);
      token.ThrowIfCancellationRequested();

      SearchItem[] items = new SearchItem[3] {
            new SearchItem("Hello World", "Lorem ipsum dolor sit amet", 0.9f),
            new SearchItem("Hello World", "Lorem ipsum dolor sit amet", 0.5f),
            new SearchItem("Hello World", "Lorem ipsum dolor sit amet", 0.1f),
        };

      return new SearchResult(items, query);
    }

    /*
     * Constructors
     *
     */

    public SearchEngine() : this(".") {}
    public SearchEngine(string source) => this.Source = source;
  }
}
