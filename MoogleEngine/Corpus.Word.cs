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
  public partial class Corpus
  {
    public class Word : System.Object
    {
      public string Self { get; private set; }
      public long Occurrences { get; set; }
      public Dictionary<Document, Source> Locations { get; private set; }

      public class Source : System.Object
      {
        public List<long> Offsets { get; private set; }

        public Source()
        {
          this.Offsets = new List<long>();
        }
      }

      public Word(string self)
      {
        this.Occurrences = 0;
        this.Locations = new Dictionary<Document, Source>();
        this.Self = self;
      }
    }
  }
}
