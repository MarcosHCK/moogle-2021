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
  public class SearchQuery
  {
    public Task? task = null;
    public CancellationTokenSource source;
    public delegate void CompletedHandler(SearchResult result);
    public event CompletedHandler? Completed;

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
                var result = Moogle.Engine.Moogle.Query(tag);
                token.ThrowIfCancellationRequested();
                if (Completed != null)
                {
                  Completed(result);
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

    public SearchQuery()
    {
      this.source = new CancellationTokenSource();
    }
  }
}
