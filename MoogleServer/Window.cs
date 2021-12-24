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
using System.Collections;
using Moogle.Engine;

namespace Moogle.Server
{
  [GLib.TypeName("MoogleServerWindow")]
  [Gtk.Template (ResourceName = "Window.ui")]
  public partial class Window : Gtk.Window
  {
    private Gtk.Dialog? About = null;
    private SearchQuery query;

    /*
     * Template childs
     *
     */

    [GtkChild]
    private Gtk.ListBox? listbox1;
    [GtkChild]
    private Gtk.SearchBar? searchbar1;
    [GtkChild]
    private Gtk.SearchEntry? searchentry1;
    [GtkChild]
    private Gtk.EntryCompletion? entrycompletion1;

    /*
     * Signals
     *
     */

    private void OnSearchCompleted(SearchResult result)
    {
      foreach (var child in listbox1!.Children)
      {
        listbox1!.Remove(child);
        child.Destroy();
      }

      foreach (var item in result.Items())
      {
        var entry = new SearchEntry(item.Title, item.Snippet);
        listbox1!.Add(entry);
        entry.Show();
      }
    }

    private void OnSearchChanged(object? widget, System.EventArgs args)
    {
      var text = searchentry1!.Text;
      if (text != "")
      {
        query.Start(text);
      }
    }

    private void OnSearchStop(object? widget, System.EventArgs args)
    {
      searchbar1!.SearchModeEnabled = false;
      query.Stop();
    }

    private void OnKeyPressEvent(object? widget, Gtk.KeyPressEventArgs args) => args.RetVal = searchbar1!.HandleEvent(args.Event);
    private void OnFocusOutEvent(object? widget, Gtk.FocusOutEventArgs args) => searchbar1!.SearchModeEnabled = false;
    private void OnShowSearch(object? widget, System.EventArgs args) => searchbar1!.SearchModeEnabled = true;

    private void OnAbout(object? widget, System.EventArgs args)
    {
      if(About == null)
      {
        var about = new Gtk.AboutDialog();
        About = about as Gtk.Dialog;
        about.TransientFor = this;

        about.Title = $"About {Server.Application.ApplicationName}";
        about.Artists = new string[] {"MarcosHCK"};
        about.Authors = new string[] {"MarcosHCK"};
        about.Copyright = "Copyright 2021-2025 MarcosHCK";
        about.Documenters = new string[] {"MarcosHCK"};
        about.License = "GNU GPLv3.0";
        about.LicenseType = Gtk.License.Gpl30;
        about.ProgramName = Server.Application.ApplicationName;
        about.TranslatorCredits = "translator-credits";
        about.Version = Server.Application.ApplicationVersion;
        about.Website = Server.Application.ApplicationWebsite;
        about.WebsiteLabel = "Github page";
        about.WrapLicense = true;

        about.DeleteEvent +=
        (Gtk.DeleteEventHandler)
        ((widget, args) =>
        {
          ((Gtk.Widget) widget).Hide();
          args.RetVal = false;
        });
      }

      About.Run();
      About.Hide();
    }

    private void OnQuit(System.Object button, System.EventArgs args) => this.Destroy();

  /*
   * Constructors
   *
   */

    public Window() : this(false) {}
    private Window(bool re) : base(null)
    {
      (new Gtk.TemplateBuilder()).InitTemplate(this);
      this.query = new SearchQuery();
      this.query.Completed += OnSearchCompleted;
      this.KeyPressEvent += OnKeyPressEvent;
      searchentry1!.FocusOutEvent += OnFocusOutEvent;
      searchbar1!.FocusOutEvent += OnFocusOutEvent;
    }
  }
}
