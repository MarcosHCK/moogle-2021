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

namespace Moogle.Engine
{
  public class SearchEngine
  {
#region Variables
    public string Source {get; private set;}
    private Corpus? corpus;

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

#region API

    public async Task<bool> Preload()
    {
      var folder = GLib.FileFactory.NewForPath (Source);
      var loader = new Corpus.Factory (typeof(SearchEngine).Assembly);
      corpus = await loader.FromFolder(folder);
    return true;
    }

    public SearchResult Query(string query)
    {
    return new SearchResult(new SearchItem[0], query);
    }

#endregion

#region Constructors

    public SearchEngine() : this(".") {}
    public SearchEngine(string source) => this.Source = source;
#endregion
  }
}
