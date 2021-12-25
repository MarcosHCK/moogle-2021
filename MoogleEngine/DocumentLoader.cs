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
  public abstract class DocumentLoader
  {
#region Implementations attribute

    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    protected sealed class MimeTypeAttribute : System.Attribute
    {
      public string MimeType { get; set;}
      public MimeTypeAttribute() => MimeType = "text/none";
    }

#endregion

#region Abstracts

    protected abstract Document LoadImplementation(GLib.IFile file, string MimeType, GLib.Cancellable? cancellable = null);

#endregion

#region Document load

    private static void Implementors(Assembly from, List<LoaderType> types)
    {
      var ttype = typeof(DocumentLoader);
      foreach(var type in from.GetTypes())
      {
        if(type.IsSubclassOf(ttype) == true)
          types.Add(new LoaderType(type));
      }
    }

    private static List<LoaderType>? types = null;
    private struct LoaderType
    {
      public Type type;
      public DocumentLoader? instance = null;
      public void Instantiate() => instance = (DocumentLoader?) Activator.CreateInstance(type);
      public LoaderType(Type type) => this.type = type;
    }

    public static Document? Load(GLib.IFile file, string MimeType, GLib.Cancellable? cancellable = null)
    {
      if (types == null)
      {
        types = new List<LoaderType>();

        /* Load implementations from this assembly */
        Implementors(typeof(DocumentLoader).Assembly, types);

        /* Maybe add a ondemand assembly load for additional loaders? */
        //Implementors(assembly, types);
      }

      foreach(var type in types)
      {
        var attr_ = type.type.GetCustomAttribute(typeof(MimeTypeAttribute));
        if (attr_ != null)
        {
          /* Check MimeType supported by this loader */
          var attr = (MimeTypeAttribute) attr_;
          if (attr.MimeType == MimeType)
          {
            if (type.instance == null)
              type.Instantiate();
            return type.instance!.LoadImplementation(file, MimeType, cancellable);
          }
        }
      }
    return null;
    }
  }

#endregion
}
