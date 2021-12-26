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
using Moogle.Engine;

namespace Moogle.Server
{
  [GLib.TypeName("MoogleServerWindow")]
  [Gtk.Template (ResourceName = "Window.ui")]
  public partial class Window : Gtk.Window
  {
#region Variables
    private Gtk.Dialog? About = null;
    private SearchQuery query;

#endregion

#region Template childs

    [GtkChild]
    private Gtk.ListBox? listbox1 = null;
    [GtkChild]
    private Gtk.SearchEntry? searchentry1 = null;

#endregion

#region Workers

    private void CleanListbox()
    {
      foreach (var child in listbox1!.Children)
      {
        listbox1!.Remove(child);
        child.Destroy();
      }
    }

#endregion

#region Signals

    private void OnSearchCompleted(SearchResult result)
    {
      /* Clean previous search's entries */
      CleanListbox();

      /* Update progress and setup a timeout */
      searchentry1!.ProgressFraction = 1f;
      GLib.Timeout.Add(500, () =>
      {
        searchentry1!.ProgressFraction = 0f;
        return false;
      });

      /* Append new search results */
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
        searchentry1!.ProgressFraction = 0.2f;
        query.Start(text);
      }
      else
      {
        searchentry1!.ProgressFraction = 0f;
        CleanListbox();
      }
    }

    private void OnSearchStop(object? widget, System.EventArgs args)
    {
      searchentry1!.ProgressFraction = 0f;
      query.Stop();
    }

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

        var logoname = $"{this.Application.ApplicationId}.svg";
        var pixbuf = new Gdk.Pixbuf(typeof(Window).Assembly, logoname);
        about.Logo = pixbuf;

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

#endregion

#region Constructors

    public Window() : this(false) {}
    private Window(bool re) : base(null)
    {
      (new Gtk.TemplateBuilder()).InitTemplate(this);
      this.query = new SearchQuery("./Content/");
      this.query.Completed += OnSearchCompleted;
    }
  }

#endregion
}
