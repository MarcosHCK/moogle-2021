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

#endregion

#region API

    public async Task<bool> Preload ()
    {
      var folder = GLib.FileFactory.NewForPath (Source);
      var loader = new Corpus.Factory (typeof (SearchEngine).Assembly);
      corpus = await loader.FromFolder (folder);
    return true;
    }

    public SearchResult Query (string query)
    {
      /* create query document */
      var vector = new Corpus.Query(query);

      /* Perform final search */
      var items = Corpus.Query.Perform (corpus!, vector);
      return new SearchResult(items, query);
    }

#endregion

#region Constructors

    public SearchEngine () : this(".") {}
    public SearchEngine (string source) => this.Source = source;
#endregion
  }
}
