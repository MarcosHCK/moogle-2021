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
  public abstract partial class Loader : System.Object, IEnumerable<(string Word, long Offset)>
  {
    public GLib.IFile Source {get; private set;}

    public abstract string GetSnippet (long offset, int wordlen, int chars_fb);

#region IEnumeratable

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
    public abstract IEnumerator<(string Word, long Offset)> GetEnumerator ();

#endregion

#region Constructors

    public Loader (GLib.IFile source)
    {
      this.Source = source;
    }

#endregion
  }
}
