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
  [GLib.TypeName ("MoogleServerWindow")]
  [Gtk.Template (ResourceName = "Window.ui")]
  public class Window : Gtk.Window
  {
#region Variables
    private Gtk.Dialog? About = null;
    private SearchEngine engine;
    private AsyncQuery query;

#endregion

#region Template childs

    [GtkChild]
    private Gtk.Grid? grid1 = null;
    [GtkChild]
    private Gtk.Image? image1 = null;
    [GtkChild]
    private Gtk.Spinner? spinner1 = null;
    [GtkChild]
    private Gtk.ListBox? listbox1 = null;
    [GtkChild]
    private Gtk.Revealer? revealer1 = null;
    [GtkChild]
    private Gtk.ModelButton? modelbutton1 = null;
    [GtkChild]
    private Gtk.SearchEntry? searchentry1 = null;

#endregion

#region Workers

    private void CleanListbox ()
    {
      foreach (var child in listbox1!.Children)
      {
        listbox1!.Remove (child);
        child.Destroy ();
      }
    }

#endregion

#region Signals

    private void OnSuggestionClose (object? widget, System.EventArgs args)
    {
      revealer1!.RevealChild = false;
    }

    private void OnGoSuggestion (object? widget, System.EventArgs args)
    {
      searchentry1!.Text = modelbutton1!.Text;
    }

    private void OnSearchCompleted (SearchResult result)
    {
      /* Clean previous search's entries */
      CleanListbox ();

      /* Update progress and setup a timeout */
      searchentry1!.ProgressFraction = 1f;
      GLib.Timeout.Add (500, () =>
      {
        searchentry1!.ProgressFraction = 0f;
        return false;
      });

      /* Append new search results */
      foreach (SearchItem item in result)
      {
        var entry = new SearchEntry (item.Title, item.Snippet);
        listbox1!.Add (entry);
        entry.Show ();
      }

      var suggest =
      result.Suggestion;
      if (suggest != "")
      {
        modelbutton1!.Text = suggest;
        revealer1!.RevealChild = true;
      }
    }

    private void OnSearchChanged (object? widget, System.EventArgs args)
    {
      var text = searchentry1!.Text;
      if (text != "")
      {
        searchentry1!.ProgressFraction = 0.2f;
        revealer1!.RevealChild = false;

        query.Start ((token) =>
        {
          SearchResult? result;
          result = engine.Query (text);
          GLib.Idle.Add (() =>
          {
            OnSearchCompleted (result);
            return false;
          });
        });
      }
      else
      {
        searchentry1!.ProgressFraction = 0f;
        CleanListbox ();
      }
    }

    private void OnSearchStop (object? widget, System.EventArgs args)
    {
      searchentry1!.ProgressFraction = 0f;
      query.Stop ();
    }

    private void OnAbout (object? widget, System.EventArgs args)
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
        var pixbuf = new Gdk.Pixbuf (typeof (Window).Assembly, logoname);
        about.Logo = pixbuf;

        about.DeleteEvent +=
        (Gtk.DeleteEventHandler)
        ((widget, args) =>
        {
          ((Gtk.Widget) widget).Hide ();
          args.RetVal = false;
        });
      }

      About.Run ();
      About.Hide ();
    }

    private void OnNotifyIcon (object? widget, GLib.NotifyArgs args)
    {
      Gdk.Pixbuf? current = this.Icon;
      if (current != null)
      {
        var pixbuf = current.ScaleSimple (16, 16, Gdk.InterpType.Bilinear);
        this.image1!.Pixbuf = pixbuf;
      }
    }

#endregion

#region Constructors

    public Window () : this (false) {}
    private Window (bool re) : base (null)
    {
      (new Gtk.TemplateBuilder ()).InitTemplate (this);
      this.engine = new SearchEngine ("./Content/");
      this.query = new AsyncQuery ();

      this.AddNotification ("icon", OnNotifyIcon);

      revealer1!.RevealChild = false;
      modelbutton1!.Text = "(null)";
      spinner1!.Visible = true;
      grid1!.Sensitive = false;
      grid1!.Visible = false;

      Task.Run (async () =>
      {
        try
        {
          await engine.Preload ();
        } catch (Exception e)
        {
          Console.Error.WriteLine (e.ToString ());
          return;
        }

        GLib.Idle.Add (() => {
          grid1!.Visible = true;
          grid1!.Sensitive = true;
          spinner1!.Visible = false;
          return false;
        });
      });
    }
  }

#endregion
}
