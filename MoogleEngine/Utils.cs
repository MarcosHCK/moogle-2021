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
  public static class Utils
  {
    public static List<Type> GetImplementors (Type super, params Assembly[] assemblies)
    {
      if (assemblies.Length == 0)
        assemblies = new Assembly[] {super.Assembly};

      var types = new List<Type> ();
      foreach (Assembly assembly in assemblies)
      {
        foreach (var type in assembly.GetTypes ())
        {
          if (type.IsSubclassOf (super) == true)
            types.Add (type);
        }
      }
    return types;
    }
  }
}
