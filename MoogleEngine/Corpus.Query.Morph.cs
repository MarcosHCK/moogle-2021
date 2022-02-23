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
using System.Text;

namespace Moogle.Engine
{
  public partial class Corpus
  {
    public partial class Query
    {
      public class Morph : Object
      {
        private StringBuilder query;
        public string Original { get; private set; }
        public string Text {
          get {
            return query.ToString ();
          }
          set {
            query.Clear ();
            query.Append (value);
          }}

        public string? Suggestion {
          get {
            var query = Original;
            var query2 = Text;
            if (query2 != query)
              return query2;
          return null;
          }
        }

        public void Alternative (Query query_, string altern, string word)
        {
          var store = query_.Words[altern];
          var offsets = store.Offsets;
          var text = query.ToString ();
          int i;

          for (i = 0; i < offsets.Count; i++)
          {
            var subst = text.Substring (offsets[i], altern.Length);
            if (subst == altern && subst != word)
            {
              query.Remove (offsets[i], altern.Length);
              query.Insert (offsets[i], word);
            }
          }
        }

        public Morph ()
        {
          query = new StringBuilder ();
          Original = "";
        }

        public Morph (string query) : this ()
        {
          this.Original = query;
          this.Text = query;
        }
      }
    }
  }
}
