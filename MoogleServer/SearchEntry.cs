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
using GtkChild = Gtk.Builder.ObjectAttribute;

namespace Moogle.Server
{
  [GLib.TypeName("MoogleServerSearchEntry")]
  [Gtk.Template(ResourceName = "SearchEntry.ui")]
  public class SearchEntry : Gtk.Grid
  {
    [GtkChild]
    private Gtk.Label? label1;
    [GtkChild]
    private Gtk.Label? label2;

    public string Title {
      get {
        return label1!.Text;
      }
      set {
        label1!.Text = value;
      }}
    public string Snippet {
      get {
        return label2!.Text;
      }
      set {
        label2!.Text = value;
      }}

    public SearchEntry(string Title, string Snippet) : this(false)
    {
      this.Title = Title;
      this.Snippet = Snippet;
    }

    private SearchEntry(bool re) : base()
    {
      (new Gtk.TemplateBuilder()).InitTemplate(this);
    }
  }
}
