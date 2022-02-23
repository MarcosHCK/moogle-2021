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
  public class SearchResult : IEnumerable, ICollection
  {
#region Variables
    private SearchItem[] items;
    public string Suggestion {get; private set;}

#endregion

#region ICollection

    public int Count {
      get {
        return items.Length;
      }}
    public bool IsSynchronized {
      get {
        return false;
      }}
    public object SyncRoot {
      get {
        return (object) this;
      }}

    public void CopyTo (Array array, int index) => items.CopyTo (array, index);

#endregion

#region IEnumerable

    public IEnumerator GetEnumerator () => items.GetEnumerator ();

#endregion

#region Constructors

    public SearchResult () : this (new SearchItem[0]) {}
    public SearchResult (SearchItem[]? items, string suggestion = "")
    {
      if (items == null)
        throw new ArgumentNullException ("items");
      this.Suggestion = suggestion;
      this.items = items;
    }
#endregion
  }
}
