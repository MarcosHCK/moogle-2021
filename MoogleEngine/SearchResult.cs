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
  public class SearchResult
  {
    private SearchItem[] items;

    public SearchResult(SearchItem[] items, string suggestion = "")
    {
      if (items == null)
      {
        throw new ArgumentNullException("items");
      }

      this.items = items;
      this.Suggestion = suggestion;
    }

    public SearchResult() : this(new SearchItem[0])
    {

    }

    public string Suggestion { get; private set; }

    public IEnumerable<SearchItem> Items()
    {
      return this.items;
    }

    public int Count { get { return this.items.Length; } }
  }
}
