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
using System.Threading;
using Moogle.Engine;

namespace Moogle.Server
{
  public class SearchQuery : GLib.Object
  {
#region Variables
    private Task? task = null;
    private CancellationTokenSource source;
    private Moogle.Engine.SearchEngine engine;

#endregion

#region Exceptions

    [System.Serializable]
    public sealed class SearchQueryException : System.Exception
    {
      public SearchQueryException() { }
      public SearchQueryException(string message) : base(message) { }
      public SearchQueryException(string message, System.Exception inner) : base(message, inner) { }
      private SearchQueryException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

#endregion

#region Events

    public delegate void CompletedHandler(SearchResult result);
    [GLib.Signal("completed")]
    public event CompletedHandler? Completed;

#endregion

#region API

    public void Start(string tag)
    {
      lock (this)
      {
        do
        {
          if (task == null)
          {
            var token = source.Token;
            task =
            new Task(async () =>
            {
              try
              {
                await Task.Delay(10, token);
                SearchResult result;
                lock (engine)
                {
                  result = this.engine.Query(tag);
                }

                token.ThrowIfCancellationRequested();
                if (Completed != null)
                {
                  GLib.Idle.Add(() =>
                  {
                    Completed(result);
                    return false;
                  });
                }
              }
              catch (OperationCanceledException)
              {
                return;
              }
            }, token);
            task.Start();
          }
          else
          {
            StopUnlocked();
          }
        }
        while (task == null);
      }
    }

    private void StopUnlocked()
    {
      if (task != null)
      {
        try
        {
          source.Cancel();
          task.Wait(source.Token);
        }
        catch (OperationCanceledException)
        {
        }

        task = null;
        if (source.TryReset() == false)
        {
          source = new CancellationTokenSource();
        }
      }
    }

    public void Stop()
    {
      lock (this)
      {
        StopUnlocked();
      }
    }

#endregion

#region Constructors

    public SearchQuery(string folder)
    {
      this.source = new CancellationTokenSource();
      this.engine = new Moogle.Engine.SearchEngine(folder);
    }
#endregion
  }
}
