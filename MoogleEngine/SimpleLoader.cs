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
  [DocumentLoader.MimeType (MimeType = "text/plain")]
  public sealed class SimpleLoader : DocumentLoader
  {
    private class SimpleDocument : Document
    {
      public byte[] buffer = new byte[128];

      public override void UdpdateTfImplementation(GLib.InputStream stream, GLib.Cancellable? cancellable = null)
      {
        lock (buffer)
        {
          long read = 0;

          do
          {
            try
            {
              read =
              stream.Read(buffer, (ulong) buffer.Length, cancellable);
              if (read == buffer.Length)
              {
              }
            }
            catch(GLib.GException e)
            {
              if(e.Domain == GLib.GioGlobal.ErrorQuark()
                && e.Code == (int) GLib.IOErrorEnum.Cancelled)
              {
                return;
              }
              else
              {
                throw;
              }
            }
          }
          while (read != 0);
        }
      }

      public SimpleDocument(GLib.IFile source) : base(source) {}
    }

    protected override Document LoadImplementation(GLib.IFile file, string MimeType, GLib.Cancellable? cancellable = null)
    {
      return new SimpleDocument(file);
    }
  }
}
