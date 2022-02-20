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
using System.Reflection;

namespace Moogle.Engine
{
  public partial class Corpus
  {
    private Dictionary<GLib.IFile, Loader> loaders = new Dictionary<GLib.IFile, Loader>();

    public string GetSnippet (GLib.IFile file, long offset, int wordlen = 5, int chars_fb = 33)
    {
      if (loaders.ContainsKey (file) == false)
        throw new ArgumentException ();
    return loaders[file].GetSnippet (offset, wordlen, chars_fb);
    }

    public class Factory : System.Object
    {
      private static Dictionary<string, Type>? loaders;
      private static Type[] arglist = {typeof (GLib.IFile)};

#region Workers

      private static async Task<bool> LoadFromImplementors (GLib.IFile file, GLib.FileInfo info, Corpus corpus)
      {
        if (loaders!.ContainsKey (info.ContentType) == false)
          return false;
        var type = loaders![info.ContentType];
        var ctor = type.GetConstructor (arglist);
        var loader_ = ctor!.Invoke ( new object[] {file} );
        var loader = (Loader) loader_;

        await Task.Run (() =>
        {
          corpus.loaders.Add (file, loader);

          foreach (var tuple in loader)
          {
            corpus.Add (tuple.Word, tuple.Offset, file);
          }
        });
      return true;
      }

      private static async Task<bool> ScanFolder (GLib.IFile folder, Corpus corpus)
      {
        var cancellable = new GLib.Cancellable();
        var token = Task.Factory.CancellationToken;
        token.Register(() => cancellable.Cancel());
        GLib.IFile child;

        var enumerator =
        folder.EnumerateChildren("standard::name,standard::content-type", 0, cancellable);
        foreach (var info in enumerator)
        {
          switch (info.FileType)
          {
          case GLib.FileType.Directory:
            child = folder.GetChild (info.Name);
            await ScanFolder (child, corpus);
            break;
          case GLib.FileType.Regular:
            child = folder.GetChild (info.Name);
            await LoadFromImplementors (child, info, corpus);
            break;
          }
        }
      return true;
      }

      public async Task<Corpus> FromFolder (GLib.IFile source)
      {
        var corpus = new Corpus();
        await ScanFolder (source, corpus);
        corpus.Postprocess ();
      return corpus;
      }

#endregion

#region Constructors

      public Factory(params Assembly[] assemblies)
      {
        if (loaders == null)
        {
          var list =
          Utils.GetImplementors (typeof(Loader), typeof(Factory).Assembly);
          loaders = new Dictionary<string, Type>();

          foreach (Type type in list)
          {
            var attrs =
            type.GetCustomAttributes (typeof (Loader.MimeTypeAttribute), false);
            foreach (Loader.MimeTypeAttribute attr in attrs)
            {
              loaders.Add (attr.MimeType, type);
            }
          }
        }
      }

#endregion
    }
  }
}
