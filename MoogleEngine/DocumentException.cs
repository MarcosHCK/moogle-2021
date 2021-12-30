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
  [System.Serializable]
  public enum DocumentExceptionCode
  {
    FAILED,
    MODIFIED,
  }

  [System.Serializable]
  public class DocumentException : System.Exception
  {
    DocumentExceptionCode code;

    public DocumentException(DocumentExceptionCode code = DocumentExceptionCode.FAILED) { }
    public DocumentException(string message, DocumentExceptionCode code = DocumentExceptionCode.FAILED) : base(message) => this.code = code;
    public DocumentException(string message, DocumentExceptionCode code, System.Exception inner) : base(message, inner) => this.code = code;
    protected DocumentException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
  }
}
