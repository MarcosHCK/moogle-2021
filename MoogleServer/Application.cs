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

namespace Moogle.Server
{
  public class Application : Gtk.Application, GLib.IInitable
  {
    public static string ApplicationName = "Moogle!";
    public static string ApplicationVersion = "1.0.0.0";
    public static string ApplicationWebsite = "https://github.com/MarcosHCK/moogle-2021/";

    /*
    * Virtuals
    *
    */

    public bool Init(GLib.Cancellable? cancellable = null)
    {
      Gtk.Application.Init();
    return true;
    }

    protected override void OnActivated()
    {
      /*
      * Initialize
      *
      */

      try
      {
        this.Init();
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e);
        ((GLib.Application) this).Quit();
      }

      /*
      * Window instantation
      *
      */

      var window = new Moogle.Server.Window();
      this.AddWindow(window);
      window.Present();
    }

    /*
    * Constructors
    *
    */

    public Application() : base(null, GLib.ApplicationFlags.None) {}
    public Application(string application_id, GLib.ApplicationFlags flags) : base(application_id, flags) {}

    /*
    * Server entry
    *
    */

    [STAThread]
    public static int Main(string[] argv)
    {
      var app = new Moogle.Server.Application("org.hck.moogle", GLib.ApplicationFlags.None);
    return app.Run(ApplicationName, argv);
    }
  }
}
